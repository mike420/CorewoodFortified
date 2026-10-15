using System.Collections.Generic;

namespace CorewoodFortified;

internal static class LadderManager
{
    private static readonly HashSet<int> EligiblePrefabs = new HashSet<int>
    {
        "mm_corewood_fortified_wall_stairs".GetStableHashCode(),
        "mm_corewood_fortified_wall_stairs_alt".GetStableHashCode(),
        "mm_corewood_fortified_wall_stairs_roofed".GetStableHashCode(),
        "mm_corewood_fortified_wall_stairs_alt_roofed".GetStableHashCode(),
    };

    // ZDO int key that lets a specific ladder opt out of custom movement (value 1 = use vanilla).
    public const int IsEligibleOverrideHash = 1026132609;
    public const int IsEligibleOverrideUseVanilla = 1;

    public static bool IsEligibleLadder(AutoJumpLedge autoJumpLedge)
    {
        ZNetView netView = autoJumpLedge.GetComponentInParent<ZNetView>();

        if (!netView || !netView.IsValid())
        {
            return false;
        }

        ZDO zdo = netView.GetZDO();

        if (ZDOExtraData.GetInt(zdo.m_uid, IsEligibleOverrideHash, out int value) && value == IsEligibleOverrideUseVanilla)
        {
            return false;
        }

        return EligiblePrefabs.Contains(zdo.GetPrefab());
    }
}
