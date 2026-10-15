using System;
using UnityEngine;

namespace CorewoodFortified;

// The ropes use the rope material from the post's coils, the trolley is the Zipline_Trolley model, and the ride
// sound is borrowed from the grappling hook
internal static class ZiplineVisuals
{
	internal static GameObject TrolleyPrefab;

	private static Material s_ropeMaterial;

	private static bool s_soundSearched;

	private static GameObject s_pullSound;

	internal static Material RopeMaterial
	{
		get
		{
			if (s_ropeMaterial == null)
			{
				Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
				s_ropeMaterial = new Material(shader) { name = "mm_zipline_rope", color = new Color(0.55f, 0.42f, 0.26f) };
				ZiplineMessages.LogWarning("The zipline post has no rope material on its coils, ropes use a plain colour.");
			}
			return s_ropeMaterial;
		}
	}

	// The coils wound round the post are rope, so the ropes between posts take their material
	internal static void FindRopeMaterial(GameObject post)
	{
		Transform coil = Utils.FindChild(post.transform, "coil_0");
		MeshRenderer renderer = coil != null ? coil.GetComponent<MeshRenderer>() : null;
		if (renderer != null && renderer.sharedMaterial != null)
		{
			s_ropeMaterial = renderer.sharedMaterial;
		}
	}

	internal static GameObject PullSoundPrefab
	{
		get
		{
			FindPullSound();
			return s_pullSound;
		}
	}

	private static void FindPullSound()
	{
		if (s_soundSearched || ObjectDB.instance == null || ZNetScene.instance == null)
		{
			return;
		}
		s_soundSearched = true;
		try
		{
			foreach (GameObject item in ObjectDB.instance.m_items)
			{
				ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
				if (drop == null || drop.m_itemData?.m_shared == null)
				{
					continue;
				}
				s_pullSound = FromProjectile(drop.m_itemData.m_shared.m_attack) ?? FromProjectile(drop.m_itemData.m_shared.m_secondaryAttack);
				if (s_pullSound != null)
				{
					return;
				}
			}
			foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
			{
				GrapplingPoint point = prefab != null ? prefab.GetComponentInChildren<GrapplingPoint>(includeInactive: true) : null;
				if (point != null && point.m_pullSound != null)
				{
					s_pullSound = point.m_pullSound;
					return;
				}
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Finding the zipline ride sound failed: " + ex.Message);
		}
	}

	private static GameObject FromProjectile(Attack attack)
	{
		if (attack == null || attack.m_attackProjectile == null)
		{
			return null;
		}
		GrapplingPoint point = attack.m_attackProjectile.GetComponentInChildren<GrapplingPoint>(includeInactive: true);
		return point != null ? point.m_pullSound : null;
	}

	internal static GameObject MakeTrolley(out Transform strap, out Transform bar)
	{
		strap = null;
		bar = null;
		if (TrolleyPrefab == null)
		{
			return null;
		}
		GameObject trolley = UnityEngine.Object.Instantiate(TrolleyPrefab);
		strap = trolley.transform.Find("strap");
		bar = trolley.transform.Find("bar");
		return trolley;
	}
}
