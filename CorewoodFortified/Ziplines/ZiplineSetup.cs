using System.Collections.Generic;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using static CorewoodFortified.PieceAssets;

namespace CorewoodFortified;

// Ziplines are based on anwo's Ziplines mod, used with permission
internal static class ZiplineSetup
{
	public const string PostPrefab = "mm_corewood_zipline_post";

	public const string RopePrefab = "mm_corewood_zipline_rope";

	// The models' names in the asset bundle. The post is renamed to PostPrefab, the name saved in worlds
	private const string PostAsset = "Zipline_Post";

	private const string TrolleyAsset = "Zipline_Trolley";

	private const string IconAsset = "Zipline";

	private const string RopePointName = "RopePoint";

	private static GameObject s_helper;

	public static void AddPieceZiplinePost()
	{
		GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(PostPrefab);
		if (!gameObject)
		{
			gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(PostAsset);
		}
		if (!gameObject)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + PostAsset + " is not in the asset bundle, skipping the zipline post.");
			return;
		}
		gameObject.name = PostPrefab;
		ZiplineVisuals.FindRopeMaterial(gameObject);
		// The icon is the prefab's own, else the Zipline sprite, else one drawn from a copy of the model. The drawing
		// is done before PreparePost adds the networked parts
		Piece existing = gameObject.GetComponent<Piece>();
		Sprite icon = existing != null ? existing.m_icon : null;
		if (icon == null)
		{
			icon = CorewoodFortifiedMod.cwfortified.LoadAsset<Sprite>(IconAsset);
		}
		if (icon == null)
		{
			gameObject.FixReferences(recursive: true);
			icon = TowerIcon.Render(gameObject);
		}
		PreparePost(gameObject);
		gameObject.GetComponent<Piece>().m_icon = icon;

		CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
		{
			Name = "$piece_mm_corewood_zipline_post",
			Description = "$piece_mm_corewood_zipline_post_description",
			CraftingStation = "forge",
			PieceTable = "_HammerPieceTable",
			Category = "CoreWood-Fortified",
			Usage = new[] { PieceUsages.Building },
			Requirements = new RequirementConfig[3]
			{
				new RequirementConfig { Item = "RoundLog", Amount = 8, Recover = true },
				new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
                new RequirementConfig { Item = "LinenThread", Amount = 10, Recover = true }
            }
		});
		Piece piece = gameObject.GetComponent<Piece>();
		piece.m_placeEffect = buildWood;
		WearNTear wearNTear = gameObject.GetComponent<WearNTear>();
		wearNTear.m_hitEffect = hitWood;
		wearNTear.m_destroyedEffect = breakWood;
		PieceManager.Instance.AddPiece(customPiece);

		AddRopePrefab();
		AddHelper();
		ZiplineVisuals.TrolleyPrefab = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(TrolleyAsset);
		if (!ZiplineVisuals.TrolleyPrefab)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + TrolleyAsset + " is not in the asset bundle, zipline riders will hang without a trolley.");
		}
	}

	// Adds what the post needs to be a piece when the prefab in Unity does not have it
	private static void PreparePost(GameObject post)
	{
		ZNetView nview = GetOrAdd<ZNetView>(post);
		nview.m_persistent = true;

		GetOrAdd<Piece>(post);

		WearNTear wearNTear = post.GetComponent<WearNTear>();
		if (!wearNTear)
		{
			wearNTear = post.AddComponent<WearNTear>();
			wearNTear.m_health = 400f;
			wearNTear.m_materialType = WearNTear.MaterialType.HardWood;
		}
		// Like a vanilla pole the post carries itself, nothing else
		wearNTear.m_supports = false;

		if (post.GetComponentInChildren<Collider>() == null)
		{
			AddCollider(post);
		}

		ZiplineAnchor anchor = GetOrAdd<ZiplineAnchor>(post);
		Transform ropePoint = Utils.FindChild(post.transform, RopePointName);
		if (ropePoint != null)
		{
			anchor.m_localRopePoint = post.transform.InverseTransformPoint(ropePoint.position);
		}
		else
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + post.name + " has no " + RopePointName + " child, ropes are tied at the default height.");
		}
	}

	// A capsule around the post's log, from the ground to the top of the pulley
	private static void AddCollider(GameObject post)
	{
		Bounds bounds = new Bounds(new Vector3(0f, 2.25f, 0f), new Vector3(0.4f, 4.5f, 0.4f));
		Transform log = Utils.FindChild(post.transform, "log");
		MeshFilter filter = log != null ? log.GetComponent<MeshFilter>() : null;
		if (filter != null && filter.sharedMesh != null)
		{
			Bounds mesh = filter.sharedMesh.bounds;
			Matrix4x4 toPost = post.transform.worldToLocalMatrix * log.localToWorldMatrix;
			bounds = new Bounds(toPost.MultiplyPoint3x4(mesh.center), Vector3.zero);
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
				bounds.Encapsulate(toPost.MultiplyPoint3x4(corner));
			}
		}
		CapsuleCollider collider = post.AddComponent<CapsuleCollider>();
		collider.direction = 1;
		collider.center = bounds.center;
		collider.height = bounds.size.y;
		collider.radius = Mathf.Max(0.1f, Mathf.Min(bounds.extents.x, bounds.extents.z));
	}

	// Each rope is its own networked object, so it lives on whichever post's area is loaded
	private static void AddRopePrefab()
	{
		GameObject rope = PrefabManager.Instance.CreateEmptyPrefab(RopePrefab, addZNetView: true);
		ZNetView nview = rope.GetComponent<ZNetView>();
		nview.m_persistent = true;
		nview.m_distant = true;
		nview.m_type = ZDO.ObjectType.Default;
		// The empty prefab comes with a cube's mesh and collider; the rope draws its own line
		foreach (Component component in rope.GetComponents<Component>())
		{
			if (component is MeshRenderer || component is MeshFilter || component is Collider)
			{
				Object.DestroyImmediate(component);
			}
		}
		rope.AddComponent<ZiplineRope>();
		PrefabManager.Instance.AddPrefab(new CustomPrefab(rope, fixReference: false));
	}

	private static void AddHelper()
	{
		if (s_helper != null)
		{
			return;
		}
		s_helper = new GameObject("mm_corewood_ziplines");
		Object.DontDestroyOnLoad(s_helper);
		s_helper.AddComponent<RopeCarry>();
		s_helper.AddComponent<RideVisuals>();
		s_helper.AddComponent<RopeGrab>();
	}

	private static T GetOrAdd<T>(GameObject gameObject) where T : Component
	{
		T component = gameObject.GetComponent<T>();
		return component ? component : gameObject.AddComponent<T>();
	}

	// Hides the post from the build menu while ziplines are turned off; placed ones stay but cannot be used
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableZiplines.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsPost);
			__instance.m_enabledPieces.RemoveWhere(IsPost);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsPost);
			}
		}
	}

	private static bool IsPost(Piece piece)
	{
		return piece != null && piece.gameObject.name == PostPrefab;
	}

	// Logging out mid-ride saves a spot on the ground instead of the point in the air
	[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SaveLogoutPoint))]
	private static class SaveLogoutPointPatch
	{
		private static void Postfix(PlayerProfile __instance)
		{
			ZiplineRider.SaveSafeLogoutPoint(__instance);
		}
	}
}
