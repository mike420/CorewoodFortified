using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CorewoodFortified;

[HarmonyPatch(typeof(MonoUpdaters))]
internal static class MonoUpdatersPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch("FixedUpdate")]
    private static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .End()
            .MatchStartBackwards(new CodeMatch(OpCodes.Ret))
            .ThrowIfInvalid("Could not patch MonoUpdaters.FixedUpdate()! (ret)")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LegacyAutoJump), nameof(LegacyAutoJump.OnFixedUpdate))))
            .InstructionEnumeration();
    }
}
