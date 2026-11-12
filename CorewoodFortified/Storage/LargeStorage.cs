using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// The large storage holds far bigger stacks than any other inventory. An item's stack size is shared by every copy of
// that item in the game, so it is left alone: the inventory code asks for the limit per inventory instead.
// The game trusts that no stack is over the normal size, and several of the ways out of a chest would delete whatever
// did not fit, so every way out below hands over normal stacks and leaves the rest where it was
internal static class LargeStorage
{
	public const string ContainerName = "$piece_mm_corewood_large_storage";

	// The game saves a stack in 16 bits, a bigger one would come back wrong
	public const int HighestStack = 65535;

	public static void Setup(GameObject piece)
	{
		Container container = piece.GetComponent<Container>();
		if (!container)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no Container, it will stack like any other chest.");
			return;
		}
		container.m_name = ContainerName;
	}

	public static bool IsLargeStorage(Inventory inventory)
	{
		return inventory != null && inventory.m_name == ContainerName;
	}

	private static bool IsOversized(ItemDrop.ItemData item)
	{
		return item.m_stack > item.m_shared.m_maxStackSize;
	}

	// Items that do not stack at all, like weapons and armour, stay that way
	public static int MaxStack(int normal, Inventory inventory)
	{
		return normal > 1 && IsLargeStorage(inventory) ? Mathf.Max(normal, MainConfig.largeStorageMaxStack.Value) : normal;
	}

	// Moves one normal stack, or the part of it that fits. False if some of it would not fit
	private static bool MoveStack(Inventory to, ItemDrop.ItemData item)
	{
		ItemDrop.ItemData stack = item.Clone();
		int size = Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize);
		stack.m_stack = size;
		bool movedAll = to.AddItem(stack);
		item.m_stack -= movedAll ? size : size - stack.m_stack;
		return movedAll;
	}

	private static void MoveStacks(Inventory to, Inventory from, ItemDrop.ItemData item)
	{
		while (item.m_stack > 0 && MoveStack(to, item))
		{
		}
		if (item.m_stack <= 0)
		{
			from.RemoveItem(item);
		}
	}

	public static void MergeStacks(Inventory inventory)
	{
		List<ItemDrop.ItemData> items = inventory.m_inventory;
		items.ForEach(LargeStorageControls.ClearMark);
		bool merged = false;
		for (int i = items.Count - 1; i > 0; i--)
		{
			ItemDrop.ItemData item = items[i];
			ItemDrop.ItemData target = items.Take(i).FirstOrDefault(other => CanMerge(other, item, inventory));
			if (target != null)
			{
				target.m_stack += item.m_stack;
				items.RemoveAt(i);
				merged = true;
			}
		}
		if (merged)
		{
			inventory.Changed();
		}
	}

	private static bool CanMerge(ItemDrop.ItemData target, ItemDrop.ItemData item, Inventory inventory)
	{
		return item.m_shared.m_maxStackSize > 1
			&& target.m_shared.m_name == item.m_shared.m_name
			&& target.m_quality == item.m_quality
			&& target.m_variant == item.m_variant
			&& target.m_crafterID == item.m_crafterID
			&& target.m_worldLevel == item.m_worldLevel
			&& (target.m_customData?.Count ?? 0) == 0
			&& (item.m_customData?.Count ?? 0) == 0
			&& target.m_stack + item.m_stack <= MaxStack(item.m_shared.m_maxStackSize, inventory);
	}

	// Multi User Chest, and the copy of it inside Azu AutoStore, work out free space from the normal stack size. Over a
	// big stack that comes out negative and the items are lost, so the chest tells them to leave it alone
	[HarmonyPatch(typeof(Container), "Awake")]
	private static class MultiUserChestIgnorePatch
	{
		private static void Postfix(Container __instance)
		{
			if (__instance.m_name != ContainerName || !__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
			{
				return;
			}
			ZDO zdo = __instance.m_nview.GetZDO();
			if (!zdo.GetBool("MUC_Ignore", false))
			{
				zdo.Set("MUC_Ignore", true);
			}
		}
	}

	// The game puts armour and materials in the bottom row first. This chest is taller than its panel, so they would
	// land out of sight below the scroll and look lost
	[HarmonyPatch(typeof(Inventory), "FindEmptySlot")]
	private static class FillFromTopPatch
	{
		private static void Prefix(Inventory __instance, ref bool topFirst)
		{
			topFirst |= IsLargeStorage(__instance);
		}
	}

	[HarmonyPatch]
	private static class StackLimitPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Inventory), "AddItem", new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) });
			// Loading a saved inventory goes through this one, which cuts every stack down to the normal size
			yield return AccessTools.Method(typeof(Inventory), "AddItem", new[] { typeof(int), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>), typeof(int), typeof(bool), typeof(bool), typeof(bool) });
			yield return AccessTools.Method(typeof(Inventory), nameof(Inventory.CanAddItem), new[] { typeof(ItemDrop.ItemData), typeof(int) });
			yield return AccessTools.Method(typeof(Inventory), nameof(Inventory.FindFreeStackSpace));
			yield return AccessTools.Method(typeof(Inventory), "FindFreeStackItem");
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo maxStackSize = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_maxStackSize));
			MethodInfo maxStack = AccessTools.Method(typeof(LargeStorage), nameof(MaxStack));
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.LoadsField(maxStackSize))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, maxStack);
				}
			}
		}
	}

	// Dragging out takes one normal stack. A swap takes the dragged item out before it knows the other side will take
	// it, so one that would send a big stack to an ordinary inventory is refused
	[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
	private static class DropItemPatch
	{
		private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, ref int amount, Vector2i pos, ref bool __result)
		{
			Inventory toInventory = __instance.GetInventory();
			if (!IsLargeStorage(toInventory))
			{
				amount = Mathf.Min(amount, item.m_shared.m_maxStackSize);
				return true;
			}
			ItemDrop.ItemData swapped = toInventory.GetItemAt(pos.x, pos.y);
			if (swapped != null && swapped != item && IsOversized(swapped) && !IsLargeStorage(fromInventory) && !swapped.IsSameType(item))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData))]
	private static class QuickMovePatch
	{
		private static bool Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
		{
			if (IsLargeStorage(__instance) || !IsOversized(item))
			{
				return true;
			}
			MoveStacks(__instance, fromInventory, item);
			__instance.Changed();
			fromInventory.Changed();
			return false;
		}
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
	private static class TakeAllPatch
	{
		private static bool Prefix(Inventory __instance, Inventory fromInventory)
		{
			if (IsLargeStorage(__instance) || !IsLargeStorage(fromInventory))
			{
				return true;
			}
			foreach (ItemDrop.ItemData item in fromInventory.GetAllItems().ToList())
			{
				MoveStacks(__instance, fromInventory, item);
			}
			__instance.Changed();
			fromInventory.Changed();
			return false;
		}
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
	private static class DropOnGroundPatch
	{
		private static void Prefix(Inventory inventory, ItemDrop.ItemData item, ref int amount)
		{
			if (IsLargeStorage(inventory))
			{
				amount = Mathf.Min(amount, item.m_shared.m_maxStackSize);
			}
		}
	}

	// A destroyed chest drops each slot as one pile. Picking one up takes what fits and leaves the rest on the ground
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
	private static class PickupPatch
	{
		private static bool Prefix(Humanoid __instance, GameObject go, bool autoPickupDelay, ref bool __result)
		{
			ItemDrop drop = go.GetComponent<ItemDrop>();
			if (!drop)
			{
				return true;
			}
			drop.Load();
			ItemDrop.ItemData item = drop.m_itemData;
			if (!IsOversized(item) || !drop.CanPickup(autoPickupDelay))
			{
				return true;
			}
			Inventory inventory = __instance.GetInventory();
			while (IsOversized(item) && inventory.CanAddItem(item, item.m_shared.m_maxStackSize) && MoveStack(inventory, item))
			{
			}
			drop.Save();
			if (!IsOversized(item))
			{
				return true;
			}
			__instance.Message(MessageHud.MessageType.Center, "$msg_noroom");
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
	private static class MergeOnOpenPatch
	{
		private static void Postfix(Container container)
		{
			if (container && IsLargeStorage(container.GetInventory()))
			{
				MergeStacks(container.GetInventory());
			}
		}
	}

	// The game draws amount/limit using the normal limit, which would read like 5000/50
	[HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
	private static class AmountTextPatch
	{
		private static void Postfix(InventoryGrid __instance)
		{
			Inventory inventory = __instance.m_inventory;
			if (!IsLargeStorage(inventory))
			{
				return;
			}
			int width = inventory.GetWidth();
			foreach (ItemDrop.ItemData item in inventory.GetAllItems())
			{
				if (item.m_shared.m_maxStackSize > 1)
				{
					__instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width).m_amount.text = item.m_stack.ToString();
				}
			}
		}
	}
}
