using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CorewoodFortified;

[HarmonyPatch(typeof(AutoJumpLedge))]
internal static class AutoJumpLedgePatch
{
    [HarmonyTranspiler]
    [HarmonyPatch("OnTriggerStay")]
    private static IEnumerable<CodeInstruction> OnTriggerStayTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Stloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Call))
            .ThrowIfInvalid("Could not patch AutoJumpLedge.OnTriggerStay()! (character-check)")
            .Advance(3)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoJumpLedgePatch), nameof(CharacterCheckDelegate))))
            .InstructionEnumeration();
    }

    // Returning null makes vanilla skip its own auto-jump for eligible ladders.
    private static Character CharacterCheckDelegate(Character character, AutoJumpLedge autoJumpLedge)
    {
        if (character
            && character == Player.m_localPlayer
            && MainConfig.modEnabled.Value
            && LadderManager.IsEligibleLadder(autoJumpLedge))
        {
            LegacyAutoJump.OnAutoJump(autoJumpLedge, character);
            return null;
        }

        return character;
    }
}
