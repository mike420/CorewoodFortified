using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodFortified;

internal static class ElevatorSetup
{
	// The prefab was set up in Unity with CoreWoodExtras' copies of these scripts. Without that mod they are
	// missing scripts and do nothing; with it they resolve to its classes, so they are removed and ours added.
	private static readonly HashSet<string> s_extrasComponents = new HashSet<string> { "Elevator", "ElevatorSwitch", "ParentPlatform", "TriggerPlatform" };

	private const string SoundPrefab = "sfx_elevator_cwf";

	private const string HandleName = "elevator_handle";

	private const string LeverName = "Lever";

	public static void Prepare(GameObject piece)
	{
		RemoveExtrasComponents(piece);

		Transform platform = FindChild(piece.transform, "platform");
		Transform[] points = FindStops(piece.transform);
		if (!platform || points.Length < 2)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " is missing its platform or has fewer than two point_ stops, the elevator will not move.");
			return;
		}

		Elevator elevator = GetOrAdd<Elevator>(platform.gameObject);
		elevator.m_points = points;
		elevator.m_soundPrefab = PrefabManager.Instance.GetPrefab(SoundPrefab);
		if (!elevator.m_soundPrefab)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Sound prefab '" + SoundPrefab + "' not found, " + piece.name + " will move silently.");
		}

		GetOrAdd<ParentPlatform>(platform.gameObject);
		GetOrAdd<TriggerPlatform>(platform.gameObject).m_platform = elevator;

		// The platform itself can be used, as well as every switch in the New and Worn models. A piece with
		// one switch names it "switch", one with a switch per floor names them "switch_a", "switch_b" and so on
		GetOrAdd<ElevatorSwitch>(platform.gameObject).m_elevator = elevator;
		foreach (Transform child in piece.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == "switch" || child.name.StartsWith("switch_"))
			{
				GetOrAdd<ElevatorSwitch>(child.gameObject).m_elevator = elevator;
			}
			// A handle per floor, "elevator_handle_01" and so on, calls the platform to that floor and pulls its lever
			else if (child.name == HandleName || child.name.StartsWith(HandleName + "_"))
			{
				ElevatorSwitch handle = GetOrAdd<ElevatorSwitch>(child.gameObject);
				handle.m_elevator = elevator;
				handle.m_lever = child.Find(LeverName);
				if (!handle.m_lever)
				{
					Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + "'s " + child.name + " has no " + LeverName + " child, it will work but not move.");
				}
			}
		}
	}

	private static void RemoveExtrasComponents(GameObject piece)
	{
		foreach (MonoBehaviour component in piece.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
		{
			// Missing scripts come back as null and cannot be removed outside the editor
			if (!component)
			{
				continue;
			}
			Type type = component.GetType();
			if (type.Assembly != typeof(Elevator).Assembly && s_extrasComponents.Contains(type.Name))
			{
				UnityEngine.Object.DestroyImmediate(component, allowDestroyingAssets: true);
			}
		}
	}

	private static T GetOrAdd<T>(GameObject gameObject) where T : Component
	{
		T component = gameObject.GetComponent<T>();
		return component ? component : gameObject.AddComponent<T>();
	}

	// Every child named point_something is a stop, ordered from the bottom up
	private static Transform[] FindStops(Transform root)
	{
		List<Transform> stops = new List<Transform>();
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name.StartsWith("point_"))
			{
				stops.Add(child);
			}
		}
		stops.Sort((a, b) => a.position.y.CompareTo(b.position.y));
		return stops.ToArray();
	}

	private static Transform FindChild(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}
}
