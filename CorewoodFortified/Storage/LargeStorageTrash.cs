using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorewoodFortified;

// The Trash tab on the large storage's panel. Items dragged onto it from the chest are recycled, with what they were
// crafted from going back into the chest, or deleted if they have no recipe
internal static class LargeStorageTrash
{
	private static readonly Color LabelColour = new Color(1f, 0.85f, 0f);

	private static RectTransform tab;
	private static Vector2 tabSize;
	private static AudioSource audio;
	private static AudioClip sound;

	// A copy of the Armor tab on the player's inventory, so it looks like the game's own. It is a child of the panel's
	// extra row, so it only shows with the row
	public static void Create(InventoryGui gui, RectTransform row)
	{
		tab = null;
		Transform armor = gui.m_player ? gui.m_player.Find("Armor") : null;
		if (!armor)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The Armor tab was not found, the large storage will have no Trash tab.");
			return;
		}
		tabSize = ((RectTransform)armor).rect.size;
		tab = (RectTransform)Object.Instantiate(armor, row, worldPositionStays: false);
		tab.name = "CorewoodFortified_Trash";
		LargeStorageControls.RemoveGamepadHints(tab.gameObject);
		foreach (UITooltip tooltip in tab.GetComponentsInChildren<UITooltip>(includeInactive: true))
		{
			Object.DestroyImmediate(tooltip);
		}
		TMP_Text label = tab.Find("ac_text")?.GetComponent<TMP_Text>();
		if (label)
		{
			label.text = LocalisedText.Get("text_large_storage_trash");
			label.color = LabelColour;
		}
		Image icon = tab.Find("armor_icon")?.GetComponent<Image>();
		Sprite sprite = LargeStoragePanel.LoadSprite("trash");
		if (icon && sprite)
		{
			icon.sprite = sprite;
			icon.color = Color.white;
			icon.preserveAspect = true;
		}
		audio = tab.gameObject.AddComponent<AudioSource>();
		audio.playOnAwake = false;
		audio.bypassReverbZones = true;
		sound = sound ? sound : LoadSound();
	}

	// Hangs off the panel's right edge, level with the top of the grid. The offset is down from the top of the row
	public static void Place(float offset)
	{
		if (!tab)
		{
			return;
		}
		tab.anchorMin = Vector2.one;
		tab.anchorMax = Vector2.one;
		tab.pivot = new Vector2(0f, 1f);
		tab.sizeDelta = tabSize;
		tab.anchoredPosition = new Vector2(0f, -offset);
	}

	private static bool PointerOverTab()
	{
		if (!tab || !tab.gameObject.activeInHierarchy)
		{
			return false;
		}
		Canvas canvas = tab.GetComponentInParent<Canvas>();
		Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
		return RectTransformUtility.RectangleContainsScreenPoint(tab, ZInput.pointerPosition, camera);
	}

	private static void TrashDragged(InventoryGui gui)
	{
		ItemDrop.ItemData item = gui.m_dragItem;
		Inventory chest = gui.m_dragInventory;
		Player player = Player.m_localPlayer;
		if (item == null || chest == null || !player || !chest.ContainsItem(item) || item.m_shared.m_questItem || !gui.m_currentContainer.IsOwner())
		{
			return;
		}
		if (!LargeStorage.IsLargeStorage(chest))
		{
			player.Message(MessageHud.MessageType.Center, "$text_large_storage_trash_chest_only");
			return;
		}
		int amount = Mathf.Min(gui.m_dragAmount, item.m_stack);
		if (amount > item.m_shared.m_maxStackSize)
		{
			Confirm(gui, item, chest, amount);
		}
		else
		{
			Trash(gui, item, chest, amount);
		}
	}

	// Anything over a normal stack can only be a big pile from this chest, and one click would lose all of it
	private static void Confirm(InventoryGui gui, ItemDrop.ItemData item, Inventory chest, int amount)
	{
		if (!UnifiedPopup.IsAvailable())
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The game's popup is not available, not trashing a big stack without asking.");
			return;
		}
		string name = Localization.instance.Localize(item.m_shared.m_name);
		string body = LocalisedText.Get(FindRecipe(item) != null ? "text_large_storage_confirm_recycle" : "text_large_storage_confirm_delete", amount.ToString(), name);
		UnifiedPopup.Push(new YesNoPopup(LocalisedText.Get("text_large_storage_confirm_header"), body, () =>
		{
			UnifiedPopup.Pop();
			// The chest may have closed or the item been moved while the question was up
			if (gui && LargeStorageControls.LargeStorageOpen(gui) && gui.m_currentContainer.GetInventory() == chest && gui.m_dragItem == item && chest.ContainsItem(item))
			{
				Trash(gui, item, chest, Mathf.Min(amount, item.m_stack));
			}
		}, UnifiedPopup.Pop, true, true));
	}

	private static void Trash(InventoryGui gui, ItemDrop.ItemData item, Inventory chest, int amount)
	{
		Player player = Player.m_localPlayer;
		int quality = item.m_quality;
		Recipe recipe = FindRecipe(item);
		int batch = recipe == null ? 1 : Mathf.Max(1, recipe.m_amount);
		int crafts = recipe == null ? 0 : amount / batch;
		if (crafts > 0)
		{
			// Only whole crafts are recycled. Dragging 25 arrows of a recipe that makes 20 leaves 5 in the chest
			amount = crafts * batch;
		}
		chest.RemoveItem(item, amount);
		if (crafts > 0)
		{
			ReturnResources(chest, recipe, crafts, quality, gui.m_currentContainer.transform.position + Vector3.up);
		}
		player.Message(MessageHud.MessageType.Center, crafts > 0 ? "$text_large_storage_recycled" : "$text_large_storage_deleted");
		PlaySound();
		gui.SetupDragItem(null, null, 1);
	}

	private static Recipe FindRecipe(ItemDrop.ItemData item)
	{
		// Raw fish is "crafted" from a whole fish, which would hand back more than it is worth
		bool rawFish = item.m_dropPrefab && item.m_dropPrefab.name.Contains("FishRaw");
		if (rawFish || MainConfig.largeStorageRecycleReturn.Value <= 0f || !ObjectDB.instance)
		{
			return null;
		}
		Recipe recipe = ObjectDB.instance.GetRecipe(item);
		return recipe != null && recipe.m_enabled ? recipe : null;
	}

	private static void ReturnResources(Inventory chest, Recipe recipe, int crafts, int quality, Vector3 dropPosition)
	{
		var resources = recipe.m_resources.Where(resource => resource.m_resItem);
		// A recipe that takes any one of its ingredients only ever cost one of them
		foreach (Piece.Requirement resource in recipe.m_requireOnlyOneIngredient ? resources.Take(1) : resources)
		{
			int perCraft = Enumerable.Range(1, quality).Sum(resource.GetAmount);
			AddToChest(chest, resource.m_resItem, Mathf.RoundToInt(perCraft * crafts * MainConfig.largeStorageRecycleReturn.Value), dropPosition);
		}
	}

	// Whatever a full chest cannot take falls on the ground beside it
	private static void AddToChest(Inventory chest, ItemDrop prefab, int amount, Vector3 dropPosition)
	{
		int stackSize = Mathf.Max(1, LargeStorage.MaxStack(prefab.m_itemData.m_shared.m_maxStackSize, chest));
		while (amount > 0)
		{
			ItemDrop.ItemData item = prefab.m_itemData.Clone();
			item.m_dropPrefab = prefab.gameObject;
			item.m_stack = Mathf.Min(stackSize, amount);
			item.m_durability = item.GetMaxDurability();
			item.m_worldLevel = (byte)Game.m_worldLevel;
			amount -= item.m_stack;
			if (!chest.AddItem(item))
			{
				ItemDrop.DropItem(item, 0, dropPosition, Quaternion.identity);
			}
		}
	}

	private static void PlaySound()
	{
		if (!audio || !sound)
		{
			return;
		}
		if (!audio.outputAudioMixerGroup && AudioMan.instance)
		{
			audio.outputAudioMixerGroup = AudioMan.instance.m_masterMixer.FindMatchingGroups("GUI").FirstOrDefault();
		}
		audio.PlayOneShot(sound, MainConfig.largeStorageTrashSoundVolume.Value);
	}

	private static AudioClip LoadSound()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CorewoodFortified.Sounds.trash.wav");
		if (stream == null)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The trash sound is missing from the mod.");
			return null;
		}
		byte[] wav = new byte[stream.Length];
		stream.Read(wav, 0, wav.Length);
		int channels = 0;
		int rate = 0;
		int bits = 0;
		for (int position = 12; position + 8 <= wav.Length;)
		{
			string chunk = System.Text.Encoding.ASCII.GetString(wav, position, 4);
			int size = System.BitConverter.ToInt32(wav, position + 4);
			int start = position + 8;
			if (chunk == "fmt ")
			{
				channels = System.BitConverter.ToInt16(wav, start + 2);
				rate = System.BitConverter.ToInt32(wav, start + 4);
				bits = System.BitConverter.ToInt16(wav, start + 14);
			}
			else if (chunk == "data" && bits == 16 && channels > 0)
			{
				float[] samples = new float[size / 2];
				for (int i = 0; i < samples.Length; i++)
				{
					samples[i] = System.BitConverter.ToInt16(wav, start + i * 2) / 32768f;
				}
				AudioClip clip = AudioClip.Create("trash", samples.Length / channels, channels, rate, stream: false);
				// Found at runtime, as this project cannot compile against SetData's newer overloads
				AccessTools.Method(typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) }).Invoke(clip, new object[] { samples, 0 });
				return clip;
			}
			position = start + size + (size & 1);
		}
		Jotunn.Logger.LogWarning("[CorewoodFortified] The trash sound is not a 16-bit WAV file.");
		return null;
	}

	// Watches the mouse rather than using a button, so nothing else on the screen can catch the click first. The press
	// is for an item picked up with a click, the release for one held and dragged across
	[HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
	private static class DragPatch
	{
		private static void Postfix(InventoryGui __instance)
		{
			if (__instance.m_dragItem == null || UnifiedPopup.IsVisible() || !LargeStorageControls.LargeStorageOpen(__instance))
			{
				return;
			}
			bool mouse = (ZInput.GetMouseButtonDown(0) || ZInput.GetMouseButtonUp(0)) && PointerOverTab();
			if (mouse || ZInput.GetKeyDown(MainConfig.largeStorageTrashKey.Value))
			{
				TrashDragged(__instance);
			}
		}
	}
}
