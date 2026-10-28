using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified.LadderAnimation;

internal sealed class OriginalIKCleanup : MonoBehaviour
{
	private const string Owner = "com.theconcernedcat.valheim.concernedladders.original-ik-cleanup";

	private static Harmony? _hook;

	private static int _count;

	private NativeIKLease? _lease;

	private bool _registered;

	internal static void Schedule(Animator animator, NativeIKLease lease)
	{
		if (animator == null || !lease.Pending)
		{
			return;
		}
		Harmony harmony = null;
		try
		{
			if (_hook == null)
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(CharacterAnimEvent), "OnAnimatorIK", new Type[1] { typeof(int) });
				if (methodInfo == null)
				{
					throw new InvalidOperationException("Native IK cleanup callback disappeared.");
				}
				harmony = new Harmony("com.theconcernedcat.valheim.concernedladders.original-ik-cleanup");
				HarmonyMethod prefix = new HarmonyMethod(typeof(OriginalIKCleanup), "BeforeNativeIK")
				{
					priority = 800
				};
				harmony.Patch(methodInfo, prefix);
				_hook = harmony;
			}
			OriginalIKCleanup originalIKCleanup = animator.GetComponent<OriginalIKCleanup>();
			if (originalIKCleanup == null)
			{
				originalIKCleanup = animator.gameObject.AddComponent<OriginalIKCleanup>();
			}
			if (originalIKCleanup._lease != null && originalIKCleanup._lease.Pending)
			{
				throw new InvalidOperationException("A previous rig cleanup must complete before a second lease.");
			}
			originalIKCleanup._lease = lease;
			if (!originalIKCleanup._registered)
			{
				originalIKCleanup._registered = true;
				_count++;
			}
		}
		catch
		{
			if (_count == 0)
			{
				try
				{
					(_hook ?? harmony)?.UnpatchSelf();
				}
				catch
				{
				}
				_hook = null;
			}
			throw;
		}
	}

	private static void BeforeNativeIK(CharacterAnimEvent __instance, int layerIndex)
	{
		if (layerIndex != 0)
		{
			return;
		}
		OriginalIKCleanup component = __instance.GetComponent<OriginalIKCleanup>();
		if (component == null || component._lease == null)
		{
			return;
		}
		Animator component2 = __instance.GetComponent<Animator>();
		if (!(component2 == null))
		{
			try
			{
				component._lease.RestoreInIK(component2);
			}
			catch
			{
				return;
			}
			component._lease = null;
			component.Unregister();
		}
	}

	private void OnDestroy()
	{
		_lease = null;
		Unregister();
	}

	private void Unregister()
	{
		if (!_registered)
		{
			return;
		}
		_registered = false;
		_count = Math.Max(0, _count - 1);
		if (_count == 0)
		{
			try
			{
				_hook?.UnpatchSelf();
			}
			catch
			{
			}
			_hook = null;
		}
	}
}
