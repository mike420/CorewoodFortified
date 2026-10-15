using System;
using System.Collections.Generic;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using static CorewoodFortified.PieceAssets;

namespace CorewoodFortified;

internal static class ChainTowerPieces
{
	public const string SmallPrefab = "mm_corewood_chain_tower_small";

	public const string MediumPrefab = "mm_corewood_chain_tower_medium";

	public const string LargePrefab = "mm_corewood_chain_tower_large";

	public static readonly int SmallHash = SmallPrefab.GetStableHashCode();

	public static readonly int MediumHash = MediumPrefab.GetStableHashCode();

	public static readonly int LargeHash = LargePrefab.GetStableHashCode();

	private static readonly HashSet<int> s_hashes = new HashSet<int> { SmallHash, MediumHash, LargeHash };

	private static readonly HashSet<GameObject> s_templates = new HashSet<GameObject>();

	public static bool IsTower(int prefab)
	{
		return s_hashes.Contains(prefab);
	}

	public static bool IsTemplate(GameObject piece)
	{
		if (piece != null)
		{
			return s_templates.Contains(piece);
		}
		return false;
	}

	public static void AddPieceChainTowerSmall()
	{
		AddTower(SmallPrefab, roundLog: 12, wood: 10, chain: 1, iron: 2);
	}

	public static void AddPieceChainTowerMedium()
	{
		AddTower(MediumPrefab, roundLog: 20, wood: 16, chain: 2, iron: 4);
	}

	public static void AddPieceChainTowerLarge()
	{
		AddTower(LargePrefab, roundLog: 30, wood: 24, chain: 3, iron: 6);
	}

	private static void AddTower(string prefab, int roundLog, int wood, int chain, int iron)
	{
		GameObject gameObject = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(prefab);
		if (!gameObject)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + prefab + " is not in the asset bundle, skipping it.");
			return;
		}
		CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
		{
			CraftingStation = "forge",
			PieceTable = "_HammerPieceTable",
			Category = "CoreWood-Fortified",
			Usage = new[] { PieceUsages.Building, PieceUsages.Defense },
			Requirements = new RequirementConfig[4]
			{
				new RequirementConfig { Item = "RoundLog", Amount = roundLog, Recover = true },
				new RequirementConfig { Item = "Wood", Amount = wood, Recover = true },
				new RequirementConfig { Item = "Chain", Amount = chain, Recover = true },
				new RequirementConfig { Item = "Iron", Amount = iron, Recover = true }
			}
		});
		gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
		SetWearEffects(gameObject, hitWood, breakWood);
		Prepare(gameObject);
		PieceManager.Instance.AddPiece(customPiece);
		s_templates.Add(gameObject);
	}

	private static void Prepare(GameObject gameObject)
	{
		Piece piece = gameObject.GetComponent<Piece>();
		if (piece.m_icon == null)
		{
			// No icon set in Unity: draw one from the model, with its JVLmock_ references resolved first.
			gameObject.FixReferences(recursive: true);
			Transform model = gameObject.transform.Find("New");
			piece.m_icon = (model != null) ? TowerIcon.Render(model.gameObject) : null;
		}
		if (gameObject.GetComponent<ChainTower>() == null)
		{
			gameObject.AddComponent<ChainTower>();
		}
	}

	private static void SetWearEffects(GameObject piece, EffectList hit, EffectList destroyed)
	{
		WearNTear wearNTear = piece.GetComponent<WearNTear>();
		if (!wearNTear)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no WearNTear component, skipping its hit and break effects.");
			return;
		}
		wearNTear.m_destroyedEffect = destroyed;
		wearNTear.m_hitEffect = hit;
	}
}
