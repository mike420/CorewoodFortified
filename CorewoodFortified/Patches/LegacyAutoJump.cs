using UnityEngine;

namespace CorewoodFortified;

internal static class LegacyAutoJump
{
    private static bool _hasAutoJumped;

    public static void OnFixedUpdate()
    {
        _hasAutoJumped = false;
    }

    public static void OnAutoJump(AutoJumpLedge autoJumpLedge, Character character)
    {
        if (_hasAutoJumped)
        {
            return;
        }

        _hasAutoJumped = true;

        float characterYaw = character.transform.rotation.eulerAngles.y;

        if (Mathf.Abs(Mathf.DeltaAngle(autoJumpLedge.transform.rotation.eulerAngles.y, characterYaw)) > 12f)
        {
            return;
        }

        Vector3 position = character.transform.position;
        position.y += character.m_running ? 0.08f : 0.06f;
        character.transform.position = position + character.transform.forward * 0.08f;
    }
}