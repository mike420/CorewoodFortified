using UnityEngine;

namespace CorewoodFortified;

// Marks a dock section, so auto docking moves a ship alongside docks only, not bridges or other pieces with posts
internal sealed class DockSection : MonoBehaviour
{
	// What every dock section is given: posts down to the ground and a deck that holds things all along it
	public static void AddTo(GameObject piece)
	{
		piece.AddComponent<ExtendingSupports>();
		piece.AddComponent<SolidDeck>();
		piece.AddComponent<DockSection>();
	}
}
