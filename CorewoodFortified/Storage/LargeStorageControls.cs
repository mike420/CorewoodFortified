using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CorewoodFortified;

// Deposit All, marking items to keep out of it, and finer control of the split dialog, while a large storage is open.
// Other chests and the game's own split dialog are left as they were
internal static class LargeStorageControls
{
	private const string KeepKey = "CorewoodFortified_Keep";
	private const string KeepFrameName = "CorewoodFortified_KeepFrame";
	private const float KeepFrameThickness = 2f;
	private const float SplitButtonSize = 36f;
	private const float SplitButtonGap = 8f;
	private static readonly Color KeepFrameColour = new Color(1f, 0.76f, 0.3f, 0.95f);

	private static GameObject splitDecrease;
	private static GameObject splitIncrease;
	private static float splitSliderWidth;

	public static bool LargeStorageOpen(InventoryGui gui)
	{
		return gui.m_currentContainer && LargeStorage.IsLargeStorage(gui.m_currentContainer.GetInventory());
	}

	private static bool IsAlwaysKept(ItemDrop.ItemData item)
	{
		return item.m_gridPos.y == 0 || item.m_equipped;
	}

	private static bool IsMarked(ItemDrop.ItemData item)
	{
		return item.m_customData != null && item.m_customData.ContainsKey(KeepKey);
	}

	// Once stored the mark means nothing, and it would stop the stack merging with others
	public static void ClearMark(ItemDrop.ItemData item)
	{
		item.m_customData?.Remove(KeepKey);
	}

	private static bool IsPlayerGrid(InventoryGrid grid)
	{
		return InventoryGui.instance && grid == InventoryGui.instance.m_playerGrid && Player.m_localPlayer && grid.m_inventory == Player.m_localPlayer.GetInventory();
	}

	public static void DepositAll()
	{
		InventoryGui gui = InventoryGui.instance;
		Player player = Player.m_localPlayer;
		if (!gui || !player || player.IsTeleporting() || !LargeStorageOpen(gui) || !gui.m_currentContainer.IsOwner())
		{
			return;
		}
		gui.SetupDragItem(null, null, 1);
		Inventory from = player.GetInventory();
		Inventory to = gui.m_currentContainer.GetInventory();
		bool moved = false;
		foreach (ItemDrop.ItemData item in from.GetAllItems().ToList())
		{
			if (IsAlwaysKept(item) || IsMarked(item) || item.m_shared.m_questItem)
			{
				continue;
			}
			int before = item.m_stack;
			if (to.AddItem(item))
			{
				from.RemoveItem(item);
				moved = true;
			}
			else
			{
				moved |= item.m_stack != before;
			}
		}
		if (moved)
		{
			from.Changed();
			to.Changed();
			gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
		}
	}

	// A copied gamepad hint would answer the original control's gamepad input
	public static void RemoveGamepadHints(GameObject copy)
	{
		foreach (UIGamePad gamePad in copy.GetComponentsInChildren<UIGamePad>(includeInactive: true))
		{
			if (gamePad.m_hint)
			{
				Object.DestroyImmediate(gamePad.m_hint);
			}
			Object.DestroyImmediate(gamePad);
		}
	}

	public static GameObject CreateButton(Button template, Transform parent, string name, string text, UnityAction onClick)
	{
		GameObject button = Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
		button.name = name;
		RemoveGamepadHints(button);
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (label)
		{
			bool capitals = !string.IsNullOrEmpty(label.text) && !label.text.Any(char.IsLower);
			label.text = capitals ? text.ToUpperInvariant() : text;
		}
		Button component = button.GetComponent<Button>();
		component.onClick = new Button.ButtonClickedEvent();
		component.onClick.AddListener(onClick);
		return button;
	}

	private static void CreateSplitButtons(SplitDialog dialog)
	{
		Slider slider = dialog.m_splitSlider;
		Button template = dialog.m_splitOkButton;
		if (!slider || !template || !slider.transform.parent)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The split dialog is not as expected, not adding its + and - buttons.");
			return;
		}
		RectTransform sliderRect = (RectTransform)slider.transform;
		splitSliderWidth = sliderRect.sizeDelta.x;
		float offset = (sliderRect.rect.width - SplitButtonRoom) / 2f + SplitButtonGap + SplitButtonSize / 2f;
		splitDecrease = CreateSplitButton(dialog, template, sliderRect, "CorewoodFortified_SplitDecrease", "-", -offset, -1);
		splitIncrease = CreateSplitButton(dialog, template, sliderRect, "CorewoodFortified_SplitIncrease", "+", offset, 1);
	}

	private static float SplitButtonRoom => (SplitButtonSize + SplitButtonGap) * 2f;

	private static GameObject CreateSplitButton(SplitDialog dialog, Button template, RectTransform sliderRect, string name, string text, float offset, int direction)
	{
		GameObject button = CreateButton(template, sliderRect.parent, name, text, () => Step(dialog, direction));
		RectTransform rect = (RectTransform)button.transform;
		rect.anchorMin = sliderRect.anchorMin;
		rect.anchorMax = sliderRect.anchorMin;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = new Vector2(SplitButtonSize, SplitButtonSize);
		Vector2 sliderCentre = sliderRect.anchoredPosition + new Vector2((0.5f - sliderRect.pivot.x) * sliderRect.rect.width, (0.5f - sliderRect.pivot.y) * sliderRect.rect.height);
		rect.anchoredPosition = sliderCentre + new Vector2(offset, 0f);
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (label)
		{
			RectTransform labelRect = (RectTransform)label.transform;
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.sizeDelta = Vector2.zero;
			labelRect.anchoredPosition = Vector2.zero;
			label.enableAutoSizing = false;
			label.fontSize = 26f;
			label.alignment = TextAlignmentOptions.Center;
		}
		button.SetActive(false);
		return button;
	}

	// Stacks here can be huge, so Shift moves by 10 and Ctrl by 100
	private static int StepSize()
	{
		if (ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl))
		{
			return 100;
		}
		return ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift) ? 10 : 1;
	}

	private static void Step(SplitDialog dialog, int direction)
	{
		Slider slider = dialog.m_splitSlider;
		dialog.SliderValue = Mathf.Clamp(slider.value + direction * StepSize(), slider.minValue, slider.maxValue);
	}

	private static void CreateKeepFrame(InventoryElement element)
	{
		GameObject frame = new GameObject(KeepFrameName, typeof(RectTransform));
		RectTransform rect = (RectTransform)frame.transform;
		rect.SetParent(element.transform, worldPositionStays: false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.one;
		rect.offsetMax = -Vector2.one;
		AddKeepFrameBar(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -KeepFrameThickness), Vector2.zero);
		AddKeepFrameBar(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, KeepFrameThickness));
		AddKeepFrameBar(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(KeepFrameThickness, 0f));
		AddKeepFrameBar(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-KeepFrameThickness, 0f), Vector2.zero);
	}

	private static void AddKeepFrameBar(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
	{
		GameObject bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
		RectTransform rect = (RectTransform)bar.transform;
		rect.SetParent(parent, worldPositionStays: false);
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.offsetMin = offsetMin;
		rect.offsetMax = offsetMax;
		Image image = bar.GetComponent<Image>();
		image.color = KeepFrameColour;
		image.raycastTarget = false;
	}

	// Place Stacks and holding Use on the chest come through here, and leave marked items in the player's inventory too
	private static List<ItemDrop.ItemData> ItemsToStack(Inventory from, Inventory to)
	{
		List<ItemDrop.ItemData> items = from.GetAllItems();
		return LargeStorage.IsLargeStorage(to) ? items.Where(item => !IsMarked(item)).ToList() : items;
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
	private static class StackAllPatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo getAllItems = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetAllItems), System.Type.EmptyTypes);
			MethodInfo itemsToStack = AccessTools.Method(typeof(LargeStorageControls), nameof(ItemsToStack));
			bool replaced = false;
			foreach (CodeInstruction instruction in instructions)
			{
				if (!replaced && instruction.Calls(getAllItems))
				{
					replaced = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
					yield return new CodeInstruction(OpCodes.Call, itemsToStack);
				}
				else
				{
					yield return instruction;
				}
			}
			if (!replaced)
			{
				Jotunn.Logger.LogWarning("[CorewoodFortified] Inventory.StackAll is not as expected, Place Stacks will not leave marked items behind.");
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "Awake")]
	private static class CreateControlsPatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			if (__instance.m_splitDialog)
			{
				CreateSplitButtons(__instance.m_splitDialog);
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "Update")]
	private static class DepositKeyPatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			if (InventoryGui.IsVisible() && LargeStorageOpen(__instance) && !__instance.m_splitDialog.IsActive
				&& !(Chat.instance && Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible()
				&& ZInput.GetKeyDown(MainConfig.largeStorageDepositKey.Value))
			{
				DepositAll();
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), "OnRightDown")]
	private static class KeepItemPatch
	{
		private static bool Prefix(InventoryGrid __instance, UIInputHandler element)
		{
			if (!ZInput.GetKey(MainConfig.largeStorageKeepKey.Value) || !IsPlayerGrid(__instance))
			{
				return true;
			}
			Vector2i pos = __instance.GetButtonPos(element.gameObject);
			ItemDrop.ItemData item = __instance.m_inventory.GetItemAt(pos.x, pos.y);
			if (item == null)
			{
				return false;
			}
			if (IsAlwaysKept(item))
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$text_large_storage_always_kept");
				return false;
			}
			if (!item.m_customData.Remove(KeepKey))
			{
				item.m_customData[KeepKey] = "1";
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
	private static class KeepFramePatch
	{
		private static void Postfix(InventoryGrid __instance)
		{
			if (!IsPlayerGrid(__instance))
			{
				return;
			}
			foreach (InventoryElement element in __instance.m_elements)
			{
				ItemDrop.ItemData item = __instance.m_inventory.GetItemAt(element.Position.x, element.Position.y);
				bool show = item != null && IsMarked(item) && !IsAlwaysKept(item);
				Transform frame = element.transform.Find(KeepFrameName);
				if (frame)
				{
					if (frame.gameObject.activeSelf != show)
					{
						frame.gameObject.SetActive(show);
					}
				}
				else if (show)
				{
					CreateKeepFrame(element);
				}
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "ShowSplitDialog")]
	private static class ShowSplitButtonsPatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			if (!splitDecrease || !splitIncrease)
			{
				return;
			}
			bool show = LargeStorageOpen(__instance);
			splitDecrease.SetActive(show);
			splitIncrease.SetActive(show);
			RectTransform slider = (RectTransform)__instance.m_splitDialog.m_splitSlider.transform;
			slider.sizeDelta = new Vector2(show ? splitSliderWidth - SplitButtonRoom : splitSliderWidth, slider.sizeDelta.y);
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "UpdateSplitDialog")]
	private static class SplitScrollPatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			SplitDialog dialog = __instance.m_splitDialog;
			if (!splitDecrease || !splitDecrease.activeSelf || !dialog.m_splitSlider.gameObject.activeInHierarchy)
			{
				return;
			}
			float scroll = ZInput.GetMouseScrollWheel();
			if (scroll != 0f)
			{
				Step(dialog, scroll > 0f ? 1 : -1);
			}
		}
	}
}
