using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// A piece resting on another, such as a torch, loses support with its distance from that piece's middle, so on a long
// piece like an 8 m dock or a bridge the ends could not hold anything. A piece with this is one solid deck, so that
// distance is taken to the nearest part of it instead. Its own support is worked out as normal.
//
// Its shape is its box colliders as built, worked out in Awake, so before ExtendingSupports stretches any posts
internal sealed class SolidDeck : MonoBehaviour
{
	private static readonly Dictionary<WearNTear, SolidDeck> s_decks = new Dictionary<WearNTear, SolidDeck>();

	// The piece whose support is being worked out, while it is
	private static WearNTear s_supported;

	private WearNTear m_wear;

	// The piece's box colliders as built, in the piece's space
	private Bounds m_deck;

	private void Awake()
	{
		m_wear = GetComponent<WearNTear>();
		if (!m_wear)
		{
			enabled = false;
			return;
		}
		m_deck = DeckBounds();
		s_decks[m_wear] = this;
	}

	private void OnDestroy()
	{
		if (m_wear)
		{
			s_decks.Remove(m_wear);
		}
	}

	private Bounds DeckBounds()
	{
		Matrix4x4 toPiece = transform.worldToLocalMatrix;
		Bounds deck = new Bounds();
		bool any = false;
		foreach (Collider collider in GetComponentsInChildren<Collider>(includeInactive: true))
		{
			if (collider.isTrigger || !(collider is BoxCollider box))
			{
				continue;
			}
			Bounds bounds = PieceBounds.Transform(new Bounds(box.center, box.size), toPiece * box.transform.localToWorldMatrix);
			if (any)
			{
				deck.Encapsulate(bounds);
			}
			else
			{
				deck = bounds;
				any = true;
			}
		}
		return deck;
	}

	// The point of the deck nearest a piece's middle
	private Vector3 NearestPointTo(Vector3 point)
	{
		return transform.TransformPoint(m_deck.ClosestPoint(transform.InverseTransformPoint(point)));
	}

	[HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
	private static class WearNTearUpdateSupportPatch
	{
		private static void Prefix(WearNTear __instance)
		{
			s_supported = __instance;
		}

		private static void Finalizer()
		{
			s_supported = null;
		}
	}

	// Asked by the piece whose support is being worked out, a deck gives the nearest part of itself as its middle. Asked
	// by itself, or by anything else, it gives its real middle
	[HarmonyPatch(typeof(WearNTear), "GetCOM")]
	private static class WearNTearGetCOMPatch
	{
		private static void Postfix(WearNTear __instance, ref Vector3 __result)
		{
			if (s_supported == null || s_supported == __instance || !s_decks.TryGetValue(__instance, out SolidDeck deck))
			{
				return;
			}
			WearNTear supported = s_supported;
			Vector3 com = supported.transform.position + supported.transform.rotation * supported.m_comOffset;
			__result = deck.NearestPointTo(com);
		}
	}
}
