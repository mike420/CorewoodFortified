using HarmonyLib;

namespace CorewoodFortified;

// A fence controller's store takes nothing but Eitr. Every way of putting an item into an inventory ends in one of
// the AddItem calls, apart from swapping two items by dragging one onto the other, which takes the dragged item out
// before it knows the other side will take it. A refused swap would lose the item, so it is stopped first
internal static class FencePatches
{
	private static bool IsFenceStore(Inventory inventory)
	{
		return inventory != null && inventory.m_name == FenceController.ContainerName;
	}

	private static bool Refuses(Inventory inventory, ItemDrop.ItemData item)
	{
		return IsFenceStore(inventory) && !FenceController.IsEitr(item);
	}

	// Only a refusal touches the result. Every prefix runs even after another mod's has skipped the method, so writing
	// true here would turn that mod's refusal into a success, and a Take All would then delete the item from the chest
	private static bool Allows(Inventory inventory, ItemDrop.ItemData item, ref bool result)
	{
		if (!Refuses(inventory, item))
		{
			return true;
		}
		result = false;
		return false;
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
	private static class AddItemPatch
	{
		private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
		{
			return Allows(__instance, item, ref __result);
		}
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
	private static class AddItemAtPatch
	{
		private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
		{
			return Allows(__instance, item, ref __result);
		}
	}

	[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
	private static class AddItemAmountPatch
	{
		private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
		{
			return Allows(__instance, item, ref __result);
		}
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
	private static class CanAddItemPatch
	{
		private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
		{
			return Allows(__instance, item, ref __result);
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
	private static class DropItemPatch
	{
		private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
		{
			Inventory toInventory = __instance.GetInventory();
			ItemDrop.ItemData swapped = toInventory.GetItemAt(pos.x, pos.y);
			if (Refuses(toInventory, item) || (swapped != null && swapped != item && Refuses(fromInventory, swapped)))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
