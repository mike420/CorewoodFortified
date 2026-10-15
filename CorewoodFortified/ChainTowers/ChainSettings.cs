namespace CorewoodFortified;

// Tuning for the chain towers. The spans are config settings, one per tower size (MainConfig, synced from the server);
// the rest are fixed.
internal static class ChainSettings
{
	public static float MinSpan = 6f;

	public static float FacingTolerance = 75f;

	public static float RaisedClearance = 1.5f;

	public static float MeshSpacing = 1.25f;

	public static float RaiseSpeed = 0.8f;

	public static float LowerSpeed = 1.5f;

	public static bool Invulnerable = false;

	public static float DamageToTowers = 0.5f;

	public static float SimulationDistance = 40f;

	public static float DrawDistance = 200f;

	public static float MaxSpan(int prefab)
	{
		if (prefab == ChainTowerPieces.LargeHash)
		{
			return MainConfig.chainMaxSpanLarge.Value;
		}
		if (prefab == ChainTowerPieces.MediumHash)
		{
			return MainConfig.chainMaxSpanMedium.Value;
		}
		return MainConfig.chainMaxSpanSmall.Value;
	}
}
