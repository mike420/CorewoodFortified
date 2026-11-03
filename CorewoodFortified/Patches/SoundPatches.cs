using System;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Audio;

namespace CorewoodFortified;

public class SoundPatches
{
	// The vanilla door sound's mixer group, so the custom sounds follow the game's sound effects volume
	private const string VanillaSfxPrefab = "sfx_door_open";

	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	private static class ZNetSceneAwakePatch
	{
		private static void Postfix()
		{
			try
			{
				GameObject vanilla = PrefabManager.Instance.GetPrefab(VanillaSfxPrefab);
				AudioSource vanillaSource = vanilla ? vanilla.GetComponentInChildren<AudioSource>() : null;
				AudioMixerGroup mixerGroup = vanillaSource ? vanillaSource.outputAudioMixerGroup : null;
				if (!mixerGroup)
				{
					Jotunn.Logger.LogWarning("[CorewoodFortified] Could not find the sound effects mixer on '" + VanillaSfxPrefab + "', custom sounds keep their default output.");
					return;
				}

				// Looked up through Jotunn so it does not matter whether its own ZNetScene patch has run yet
				foreach (string prefabName in CorewoodFortifiedMod.SfxPrefabs)
				{
					GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
					AudioSource audioSource = prefab ? prefab.GetComponentInChildren<AudioSource>() : null;
					if (!audioSource)
					{
						Jotunn.Logger.LogWarning("[CorewoodFortified] No AudioSource found on '" + prefabName + "', skipping mixer assignment.");
						continue;
					}
					audioSource.outputAudioMixerGroup = mixerGroup;
				}
			}
			catch (Exception ex)
			{
				Jotunn.Logger.LogError("Error with SoundPatches ZNetSceneAwakePatch: " + ex);
			}
		}
	}

	// The large drawbridges share their sounds with the lift gate, so their volume is picked by the door playing them
	private static readonly string[] LargeDrawbridgePrefabs =
	{
		"mm_corewood_fortified_drawbridge_large",
		"mm_corewood_fortified_drawbridge_roofed",
		"mm_corewood_drawbridge_large"
	};

	// True while a large drawbridge's Door.SetState runs, which is where its open and close sounds are created
	private static bool s_largeDrawbridgePlaying;

	[HarmonyPatch(typeof(Door), "SetState")]
	private static class DoorSetStatePatch
	{
		private static void Prefix(Door __instance)
		{
			s_largeDrawbridgePlaying = Array.IndexOf(LargeDrawbridgePrefabs, Utils.GetPrefabName(__instance.gameObject)) >= 0;
		}

		// A finalizer runs even if SetState throws, so the flag never leaks onto another door's sounds
		private static void Finalizer()
		{
			s_largeDrawbridgePlaying = false;
		}
	}

	// The gate and drawbridge sounds, the only ones the gate volume settings scale. The other custom sounds have
	// their own volume settings, applied where they are played
	private static readonly string[] GateSfxPrefabs =
	{
		"sfx_open_fortified_cwf",
		"sfx_close_fortified_cwf",
		"sfx_oc_drawbridge_cwf",
		"sfx_open_drawbridge_cwf",
		"sfx_close_drawbridge_cwf"
	};

	// The gate and drawbridge sounds are scaled by the player's volume setting as each one spawns.
	// The elevator plays its sound from the platform instead (see Elevator), and applies its own volume setting there.
	[HarmonyPatch(typeof(ZSFX), "Awake")]
	private static class ZSFXAwakePatch
	{
		private static void Postfix(ZSFX __instance)
		{
			string prefabName = Utils.GetPrefabName(__instance.gameObject);
			if (Array.IndexOf(GateSfxPrefabs, prefabName) < 0)
			{
				return;
			}
			__instance.SetVolumeModifier(s_largeDrawbridgePlaying ? MainConfig.largeDrawbridgeSoundVolume.Value : MainConfig.gateSoundVolume.Value);
		}
	}
}
