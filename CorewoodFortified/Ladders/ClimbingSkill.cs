using System;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodFortified;

// The Climbing skill: rises with every metre climbed, makes fast climbing and sliding cheaper on stamina
// and unlocks sliding down. Ported from The Concerned Cat's Concerned Ladders
internal static class ClimbingSkill
{
	private const string Identifier = "MagicMike.CorewoodFortified.climbing";

	// Skill raised per metre climbed, up or down
	private const float RaisePerMetre = 0.25f;

	// At level 100 fast climbing and sliding cost this much less stamina
	private const float MaxStaminaSaving = 0.25f;

	internal static Skills.SkillType Type { get; private set; }

	private static bool Ready => Type != Skills.SkillType.None;

	internal static void Register()
	{
		if (Ready)
		{
			return;
		}
		try
		{
			GameObject ladder = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>("mm_corewood_fortified_ladder_2m");
			Piece piece = ladder != null ? ladder.GetComponent<Piece>() : null;
			Type = SkillManager.Instance.AddSkill(new SkillConfig
			{
				Identifier = Identifier,
				Name = "$skill_cwf_climbing",
				Description = "$skill_cwf_climbing_description",
				Icon = piece != null ? piece.m_icon : null,
				IncreaseStep = 1f
			});
		}
		catch (Exception ex)
		{
			Type = Skills.SkillType.None;
			Jotunn.Logger.LogWarning("[CorewoodFortified] Adding the Climbing skill failed, ladders work without it: " + ex.Message);
		}
	}

	// 0 to 100
	internal static float Level(Player player)
	{
		return Ready && player != null ? player.GetSkillLevel(Type) : 0f;
	}

	internal static float StaminaScale(Player player)
	{
		return 1f - MaxStaminaSaving * Mathf.Clamp(Level(player), 0f, 100f) / 100f;
	}

	internal static void Raise(Player player, int metres)
	{
		if (!Ready || player == null)
		{
			return;
		}
		for (int i = 0; i < metres; i++)
		{
			player.RaiseSkill(Type, RaisePerMetre);
		}
	}
}
