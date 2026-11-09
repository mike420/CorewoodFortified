using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Jotunn.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorewoodFortified;

// The extra row of the large storage's panel: search on the left, Deposit All in the middle, sorting on the right.
// The panel is shared with every other chest, so it grows to fit the row only while a large storage is open, and is
// put back exactly as it was afterwards
internal static class LargeStoragePanel
{
	private enum SortMode { None, Name, Weight, Type }

	private sealed class SavedRect
	{
		public RectTransform Rect;
		public Vector2 OffsetMin;
		public Vector2 OffsetMax;
	}

	private const float Gap = 4f;
	private const float DimmedAlpha = 0.2f;
	private const float ClearButtonWidth = 24f;
	private const int VisibleRows = 5;
	private const float ScreenMargin = 20f;
	private static readonly Vector3[] Corners = new Vector3[4];

	private static RectTransform row;
	private static RectTransform searchRect;
	private static RectTransform depositRect;
	private static readonly List<RectTransform> sortRects = new List<RectTransform>();
	private static TMP_InputField search;
	private static GameObject clearButton;
	private static List<SavedRect> savedLayout;
	private static bool expanded;

	private static string[] searchTerms = new string[0];
	private static SortMode lastSort;
	private static bool reversed;
	private static Dictionary<string, string> modsByPrefab;
	private static readonly Dictionary<string, string> ingredientsByItem = new Dictionary<string, string>();

	public static bool SearchFocused => search != null && search.isFocused;

	private static void Create(InventoryGui gui)
	{
		row = null;
		search = null;
		searchRect = null;
		clearButton = null;
		savedLayout = null;
		expanded = false;
		sortRects.Clear();
		if (!gui.m_takeAllButton || !gui.m_stackAllButton || !gui.m_container || !gui.m_containerGrid || gui.m_containerGrid.transform.parent != gui.m_container)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The chest panel is not as expected, not adding the large storage's search and sort row.");
			return;
		}
		row = new GameObject("CorewoodFortified_StorageRow", typeof(RectTransform)).GetComponent<RectTransform>();
		row.SetParent(gui.m_container, worldPositionStays: false);
		depositRect = (RectTransform)LargeStorageControls.CreateButton(gui.m_takeAllButton, row, "CorewoodFortified_DepositAll", LocalisedText.Get("text_large_storage_deposit_all"), LargeStorageControls.DepositAll).transform;
		// Right to left, as they are placed from the panel's right edge
		CreateSortButton(gui, "sort_type", SortMode.Type);
		CreateSortButton(gui, "sort_weight", SortMode.Weight);
		CreateSortButton(gui, "sort_name", SortMode.Name);
		LargeStorageTrash.Create(gui, row);
		row.gameObject.SetActive(false);
	}

	private static void CreateSortButton(InventoryGui gui, string image, SortMode mode)
	{
		GameObject button = LargeStorageControls.CreateButton(gui.m_takeAllButton, row, "CorewoodFortified_" + image, "", () => Sort(mode));
		sortRects.Add((RectTransform)button.transform);
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		Color tint = label ? label.color : Color.white;
		if (label)
		{
			label.gameObject.SetActive(false);
		}
		Sprite sprite = LoadSprite(image);
		if (!sprite)
		{
			return;
		}
		RectTransform icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
		icon.SetParent(button.transform, worldPositionStays: false);
		icon.anchorMin = new Vector2(0.14f, 0.14f);
		icon.anchorMax = new Vector2(0.86f, 0.86f);
		icon.offsetMin = Vector2.zero;
		icon.offsetMax = Vector2.zero;
		Image iconImage = icon.GetComponent<Image>();
		iconImage.sprite = sprite;
		iconImage.color = tint;
		iconImage.preserveAspect = true;
		iconImage.raycastTarget = false;
	}

	public static Sprite LoadSprite(string name)
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CorewoodFortified.Images." + name + ".png");
		if (stream == null)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The image " + name + " is missing from the mod.");
			return null;
		}
		byte[] bytes = new byte[stream.Length];
		stream.Read(bytes, 0, bytes.Length);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
		// Found at runtime, as this project cannot compile against Unity's image module
		AccessTools.Method(AccessTools.TypeByName("UnityEngine.ImageConversion"), "LoadImage", new[] { typeof(Texture2D), typeof(byte[]) }).Invoke(null, new object[] { texture, bytes });
		return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
	}

	// A copy of the build menu's search box, so it looks like the game's own. Made on first use, as the build menu may
	// not exist yet when the inventory wakes. The row is still hidden here, so the copy has not started: everything it
	// was wired to can be replaced before it does
	private static void CreateSearch(InventoryGui gui)
	{
		TMP_InputField source = Hud.instance && Hud.instance.m_buildUi ? Hud.instance.m_buildUi.m_searchField : null;
		if (!source)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The build menu's search box was not found, the large storage will have no search box.");
			return;
		}
		TMP_InputField input = UnityEngine.Object.Instantiate(source, row, worldPositionStays: false);
		input.name = "CorewoodFortified_Search";
		input.gameObject.SetActive(true);
		searchRect = (RectTransform)input.transform;
		input.onValueChanged = new TMP_InputField.OnChangeEvent();
		input.onEndEdit = new TMP_InputField.SubmitEvent();
		input.onSubmit = new TMP_InputField.SubmitEvent();
		input.onSelect = new TMP_InputField.SelectionEvent();
		input.onDeselect = new TMP_InputField.SelectionEvent();
		input.navigation = new Navigation { mode = Navigation.Mode.None };
		LargeStorageControls.RemoveGamepadHints(input.gameObject);
		// A box that has been shown already carries its caret, and the copy makes its own
		foreach (TMP_SelectionCaret caret in input.GetComponentsInChildren<TMP_SelectionCaret>(includeInactive: true))
		{
			UnityEngine.Object.DestroyImmediate(caret.gameObject);
		}
		input.text = "";
		if (input.placeholder is TMP_Text placeholder)
		{
			placeholder.text = LocalisedText.Get("text_large_storage_search");
		}
		if (input.textViewport)
		{
			input.textViewport.offsetMax -= new Vector2(ClearButtonWidth, 0f);
		}
		input.onValueChanged.AddListener(SetSearch);
		search = input;
		CreateClearButton(gui);
	}

	private static void CreateClearButton(InventoryGui gui)
	{
		RectTransform rect = new GameObject("Clear", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
		rect.SetParent(searchRect, worldPositionStays: false);
		rect.anchorMin = new Vector2(1f, 0f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 0.5f);
		rect.sizeDelta = new Vector2(ClearButtonWidth, 0f);
		rect.anchoredPosition = Vector2.zero;
		// Invisible, but it is what catches the click
		rect.GetComponent<Image>().color = Color.clear;

		TextMeshProUGUI text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
		text.rectTransform.SetParent(rect, worldPositionStays: false);
		text.rectTransform.anchorMin = Vector2.zero;
		text.rectTransform.anchorMax = Vector2.one;
		text.rectTransform.offsetMin = Vector2.zero;
		text.rectTransform.offsetMax = Vector2.zero;
		TMP_Text template = gui.m_takeAllButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (template)
		{
			text.font = template.font;
			text.color = template.color;
		}
		text.fontSize = 18f;
		text.alignment = TextAlignmentOptions.Center;
		text.raycastTarget = false;
		text.text = "X";

		Button button = rect.GetComponent<Button>();
		button.targetGraphic = text;
		button.navigation = new Navigation { mode = Navigation.Mode.None };
		button.onClick.AddListener(() => search.text = "");
		clearButton = rect.gameObject;
		clearButton.SetActive(false);
	}

	private static void SetSearch(string text)
	{
		searchTerms = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (clearButton)
		{
			clearButton.SetActive(text.Length > 0);
		}
	}

	private static float Top(RectTransform parent, RectTransform rect)
	{
		rect.GetWorldCorners(Corners);
		return parent.rect.yMax - parent.InverseTransformPoint(Corners[1]).y;
	}

	private static float Bottom(RectTransform parent, RectTransform rect)
	{
		rect.GetWorldCorners(Corners);
		return parent.rect.yMax - parent.InverseTransformPoint(Corners[0]).y;
	}

	private static SavedRect Save(RectTransform rect)
	{
		return new SavedRect { Rect = rect, OffsetMin = rect.offsetMin, OffsetMax = rect.offsetMax };
	}

	private static void SetExpanded(InventoryGui gui, bool expand)
	{
		expanded = expand;
		if (!expand)
		{
			foreach (SavedRect saved in savedLayout ?? new List<SavedRect>())
			{
				saved.Rect.offsetMin = saved.OffsetMin;
				saved.Rect.offsetMax = saved.OffsetMax;
			}
			row.gameObject.SetActive(false);
			return;
		}
		if (!search)
		{
			CreateSearch(gui);
		}
		RectTransform panel = gui.m_container;
		List<RectTransform> children = panel.Cast<Transform>().Select(child => (RectTransform)child).Where(child => child != row).ToList();
		savedLayout = children.Select(Save).Append(Save(panel)).ToList();
		RectTransform takeAll = (RectTransform)gui.m_takeAllButton.transform;
		RectTransform stackAll = (RectTransform)gui.m_stackAllButton.transform;
		RectTransform grid = (RectTransform)gui.m_containerGrid.transform;
		Scrollbar scrollbar = gui.m_containerGrid.m_scrollbar;
		float rowHeight = takeAll.rect.height;
		float rowTop = Bottom(panel, takeAll) + Gap;
		float gridTop = Top(panel, grid);
		float shift = Mathf.Max(0f, rowTop + rowHeight + Gap - gridTop);
		float tallerGrid = Mathf.Max(0f, VisibleRows * gui.m_containerGrid.m_elementSpace - grid.rect.height);
		float growth = shift + Mathf.Min(tallerGrid, Mathf.Max(0f, RoomBelow(panel) - shift));
		PlaceRow(panel, takeAll, stackAll, rowTop, rowHeight);
		LargeStorageTrash.Place(gridTop + shift - rowTop);
		foreach (RectTransform child in children)
		{
			bool stretched = !Mathf.Approximately(child.anchorMin.y, child.anchorMax.y);
			bool movesWithGrid = child == grid || (scrollbar && child == scrollbar.transform);
			if (stretched && !movesWithGrid)
			{
				continue;
			}
			// Growing the panel downwards drags each child by how far down the panel it is anchored. What is above the
			// grid stays put. The grid's top makes room for the row and its bottom follows the panel's, and whatever
			// is below the grid follows the panel's bottom too
			bool below = movesWithGrid || (Top(panel, child) + Bottom(panel, child)) / 2f > gridTop;
			float topMove = movesWithGrid ? shift : below ? growth : 0f;
			float bottomMove = below ? growth : 0f;
			child.offsetMax += Vector2.up * ((1f - child.anchorMax.y) * growth - topMove);
			child.offsetMin += Vector2.up * ((1f - child.anchorMin.y) * growth - bottomMove);
		}
		panel.offsetMin += Vector2.down * growth;
		row.gameObject.SetActive(true);
	}

	// How far the panel can grow before its bottom edge nears the bottom of the screen
	private static float RoomBelow(RectTransform panel)
	{
		Canvas canvas = panel.GetComponentInParent<Canvas>();
		if (!canvas || panel.lossyScale.y <= 0f)
		{
			return float.MaxValue;
		}
		panel.GetWorldCorners(Corners);
		float panelBottom = Corners[0].y;
		((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(Corners);
		return (panelBottom - Corners[0].y) / panel.lossyScale.y - ScreenMargin;
	}

	private static void PlaceRow(RectTransform panel, RectTransform takeAll, RectTransform stackAll, float rowTop, float rowHeight)
	{
		row.anchorMin = new Vector2(0f, 1f);
		row.anchorMax = new Vector2(1f, 1f);
		row.offsetMin = new Vector2(0f, -rowTop - rowHeight);
		row.offsetMax = new Vector2(0f, -rowTop);
		takeAll.GetWorldCorners(Corners);
		float left = panel.InverseTransformPoint(Corners[0]).x - panel.rect.xMin;
		stackAll.GetWorldCorners(Corners);
		float right = panel.rect.xMax - panel.InverseTransformPoint(Corners[2]).x;
		float buttonWidth = takeAll.rect.width;

		Place(depositRect, 0.5f, 0.5f, 0f, new Vector2(buttonWidth, rowHeight));
		float room = (panel.rect.width - buttonWidth) / 2f - left - Gap;
		if (searchRect)
		{
			Place(searchRect, 0f, 0f, left, new Vector2(Mathf.Min(buttonWidth * 1.3f, room), rowHeight));
		}
		for (int i = 0; i < sortRects.Count; i++)
		{
			Place(sortRects[i], 1f, 1f, -right - i * (rowHeight + Gap), new Vector2(rowHeight, rowHeight));
		}
	}

	private static void Place(RectTransform rect, float anchorX, float pivotX, float x, Vector2 size)
	{
		rect.anchorMin = new Vector2(anchorX, 0.5f);
		rect.anchorMax = new Vector2(anchorX, 0.5f);
		rect.pivot = new Vector2(pivotX, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = new Vector2(x, 0f);
	}

	private static string LocalName(ItemDrop.ItemData item)
	{
		return Localization.instance.Localize(item.m_shared.m_name);
	}

	private static int Group(ItemDrop.ItemData item)
	{
		switch (item.m_shared.m_itemType)
		{
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
			case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
			case ItemDrop.ItemData.ItemType.Bow:
			case ItemDrop.ItemData.ItemType.Attach_Atgeir:
			case ItemDrop.ItemData.ItemType.Torch:
				return 0;
			case ItemDrop.ItemData.ItemType.Shield:
				return 1;
			case ItemDrop.ItemData.ItemType.Helmet:
			case ItemDrop.ItemData.ItemType.Chest:
			case ItemDrop.ItemData.ItemType.Legs:
			case ItemDrop.ItemData.ItemType.Hands:
			case ItemDrop.ItemData.ItemType.Shoulder:
			case ItemDrop.ItemData.ItemType.Utility:
			case ItemDrop.ItemData.ItemType.Trinket:
				return 2;
			case ItemDrop.ItemData.ItemType.Ammo:
			case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
				return 3;
			case ItemDrop.ItemData.ItemType.Tool:
				return 4;
			case ItemDrop.ItemData.ItemType.Consumable:
				return 5;
			case ItemDrop.ItemData.ItemType.Material:
				return 6;
			case ItemDrop.ItemData.ItemType.Trophy:
				return 7;
			case ItemDrop.ItemData.ItemType.Fish:
				return 8;
			default:
				return 9;
		}
	}

	// The words @ searches match for each group above, in the same order
	private static readonly string[] GroupWords = { "weapon", "shield", "armor armour", "ammo", "tool", "consumable food", "material", "trophy", "fish", "misc" };

	private static void Sort(SortMode mode)
	{
		InventoryGui gui = InventoryGui.instance;
		Player player = Player.m_localPlayer;
		if (!gui || !player || player.IsTeleporting() || !LargeStorageControls.LargeStorageOpen(gui) || !gui.m_currentContainer.IsOwner())
		{
			return;
		}
		gui.SetupDragItem(null, null, 1);
		reversed = mode == lastSort && mode != SortMode.Type && !reversed;
		lastSort = mode;
		Inventory inventory = gui.m_currentContainer.GetInventory();
		LargeStorage.MergeStacks(inventory);
		int width = inventory.GetWidth();
		int index = 0;
		foreach (ItemDrop.ItemData item in Sorted(inventory.GetAllItems(), mode).ToList())
		{
			item.m_gridPos = new Vector2i(index % width, index / width);
			index++;
		}
		inventory.Changed();
		gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
	}

	private static IEnumerable<ItemDrop.ItemData> Sorted(IEnumerable<ItemDrop.ItemData> items, SortMode mode)
	{
		StringComparer names = StringComparer.CurrentCultureIgnoreCase;
		switch (mode)
		{
			case SortMode.Name:
				return reversed ? items.OrderByDescending(LocalName, names) : items.OrderBy(LocalName, names);
			case SortMode.Weight:
				return (reversed ? items.OrderBy(item => item.GetWeight()) : items.OrderByDescending(item => item.GetWeight())).ThenBy(LocalName, names);
			default:
				return items.OrderBy(Group).ThenBy(item => (int)item.m_shared.m_itemType).ThenBy(LocalName, names);
		}
	}

	private static bool Matches(ItemDrop.ItemData item)
	{
		foreach (string term in searchTerms)
		{
			bool match = term[0] == '@' ? MatchesTag(item, term.Substring(1)) : LocalName(item).ToLowerInvariant().Contains(term);
			if (!match)
			{
				return false;
			}
		}
		return true;
	}

	// @ matches the mod an item came from, the kind of item, or what it is crafted from. "@mod" alone is any modded item
	private static bool MatchesTag(ItemDrop.ItemData item, string tag)
	{
		if (tag.Length == 0)
		{
			return true;
		}
		string mod = ModOf(item);
		return (mod != null && (tag == "mod" || mod.Contains(tag)))
			|| GroupWords[Group(item)].Contains(tag)
			|| IngredientsOf(item).Contains(tag);
	}

	private static string ModOf(ItemDrop.ItemData item)
	{
		if (modsByPrefab == null)
		{
			modsByPrefab = new Dictionary<string, string>();
			foreach (ModRegistry.ModInfo info in ModRegistry.GetMods())
			{
				foreach (var customItem in info.Items)
				{
					modsByPrefab[customItem.ItemPrefab.name] = (info.Name + " " + info.GUID).ToLowerInvariant();
				}
			}
		}
		return item.m_dropPrefab && modsByPrefab.TryGetValue(item.m_dropPrefab.name, out string mod) ? mod : null;
	}

	private static string IngredientsOf(ItemDrop.ItemData item)
	{
		string name = item.m_shared.m_name;
		if (!ingredientsByItem.TryGetValue(name, out string ingredients))
		{
			Recipe recipe = ObjectDB.instance ? ObjectDB.instance.GetRecipe(item) : null;
			ingredients = recipe == null ? "" : string.Join(" ", recipe.m_resources.Where(resource => resource.m_resItem).Select(resource => LocalName(resource.m_resItem.m_itemData))).ToLowerInvariant();
			ingredientsByItem[name] = ingredients;
		}
		return ingredients;
	}

	[HarmonyPatch(typeof(InventoryGui), "Awake")]
	private static class CreatePatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			Create(__instance);
		}
	}

	// Before the game draws the grid, so it is drawn in its new place from the first frame
	[HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
	private static class LayoutPatch
	{
		private static void Prefix(InventoryGui __instance)
		{
			bool open = LargeStorageControls.LargeStorageOpen(__instance);
			if (row && open != expanded)
			{
				SetExpanded(__instance, open);
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
	private static class ResetOnOpenPatch
	{
		private static void Postfix()
		{
			lastSort = SortMode.None;
			reversed = false;
			modsByPrefab = null;
			ingredientsByItem.Clear();
			if (search)
			{
				search.text = "";
			}
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
	private static class DimPatch
	{
		private static void Postfix(InventoryGrid __instance)
		{
			Inventory inventory = __instance.m_inventory;
			if (!LargeStorage.IsLargeStorage(inventory))
			{
				return;
			}
			int width = inventory.GetWidth();
			foreach (ItemDrop.ItemData item in inventory.GetAllItems())
			{
				InventoryElement element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
				bool match = Matches(item);
				element.m_amount.alpha = match ? 1f : DimmedAlpha;
				if (!match)
				{
					element.m_icon.color = new Color(1f, 1f, 1f, DimmedAlpha);
				}
			}
		}
	}

	// Every key the game reads goes through these. Without this, typing an E in the search box would close the chest
	[HarmonyPatch]
	private static class BlockKeysWhileTypingPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetButton));
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetButtonDown));
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetButtonUp));
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetKey));
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetKeyDown));
			yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.GetKeyUp));
		}

		private static bool Prefix(ref bool __result)
		{
			if (!SearchFocused)
			{
				return true;
			}
			__result = false;
			return false;
		}
	}
}
