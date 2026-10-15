using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodFortified;

internal static class TowerIcon
{
	private const int Size = 128;

	private static readonly Vector3 Away = new Vector3(0f, 5000f, 0f);

	private const int Layer = 31;

	private static readonly List<UnityEngine.Object> s_made = new List<UnityEngine.Object>();

	public static Sprite Render(GameObject model)
	{
		GameObject gameObject = null;
		GameObject gameObject2 = null;
		RenderTexture renderTexture = null;
		RenderTexture active = RenderTexture.active;
		List<Light> list = new List<Light>();
		bool fog = RenderSettings.fog;
		AmbientMode ambientMode = RenderSettings.ambientMode;
		Color ambientLight = RenderSettings.ambientLight;
		try
		{
			gameObject = UnityEngine.Object.Instantiate(model, Away, Quaternion.identity);
			LODGroup[] componentsInChildren = gameObject.GetComponentsInChildren<LODGroup>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
			}
			Bounds bounds = new Bounds(Away, Vector3.zero);
			bool flag = false;
			Transform[] componentsInChildren2 = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].gameObject.layer = 31;
			}
			Renderer[] componentsInChildren3 = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren3)
			{
				if (flag)
				{
					bounds.Encapsulate(renderer.bounds);
				}
				else
				{
					bounds = renderer.bounds;
				}
				flag = true;
			}
			if (!flag)
			{
				return null;
			}
			gameObject2 = new GameObject("CWChain_IconRig");
			Camera camera = gameObject2.AddComponent<Camera>();
			camera.enabled = false;
			camera.cullingMask = int.MinValue;
			camera.clearFlags = CameraClearFlags.Color;
			camera.fieldOfView = 24f;
			camera.renderingPath = RenderingPath.Forward;
			camera.allowHDR = false;
			camera.allowMSAA = false;
			Vector3 normalized = new Vector3(0.75f, 0.55f, 1f).normalized;
			float magnitude = bounds.extents.magnitude;
			float num = magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * (Mathf.PI / 180f)) * 0.92f;
			camera.transform.position = bounds.center + normalized * num;
			camera.transform.LookAt(bounds.center);
			camera.nearClipPlane = Mathf.Max(0.05f, num - magnitude * 2f);
			camera.farClipPlane = num + magnitude * 2f;
			GameObject gameObject3 = new GameObject("light");
			gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			gameObject3.transform.rotation = Quaternion.LookRotation(new Vector3(-0.5f, -0.8f, -0.6f));
			Light light = gameObject3.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.1f;
			light.color = new Color(1f, 0.96f, 0.9f);
			light.cullingMask = int.MinValue;
			light.shadows = LightShadows.None;
			Light[] array = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
			foreach (Light light2 in array)
			{
				if (light2 != light && light2.enabled && light2.type == LightType.Directional)
				{
					light2.enabled = false;
					list.Add(light2);
				}
			}
			RenderSettings.fog = false;
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
			renderTexture = (camera.targetTexture = RenderTexture.GetTemporary(128, 128, 24, RenderTextureFormat.ARGB32));
			Color[] array2 = Draw(camera, renderTexture, Color.black);
			Color[] array3 = Draw(camera, renderTexture, Color.white);
			Color[] array4 = new Color[array2.Length];
			for (int j = 0; j < array4.Length; j++)
			{
				Color color = array2[j];
				Color color2 = array3[j];
				float num2 = Mathf.Clamp01(1f - (color2.r - color.r + (color2.g - color.g) + (color2.b - color.b)) / 3f);
				array4[j] = ((num2 > 0.004f) ? new Color(color.r / num2, color.g / num2, color.b / num2, num2) : Color.clear);
			}
			Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false)
			{
				name = "CWChain_icon_" + model.transform.parent.name
			};
			texture2D.SetPixels(array4);
			texture2D.Apply();
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f), 100f);
			sprite.name = texture2D.name;
			s_made.Add(texture2D);
			s_made.Add(sprite);
			return sprite;
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogWarning("tower picture not drawn: " + ex.Message);
			return null;
		}
		finally
		{
			foreach (Light item in list)
			{
				if (item != null)
				{
					item.enabled = true;
				}
			}
			RenderSettings.fog = fog;
			RenderSettings.ambientMode = ambientMode;
			RenderSettings.ambientLight = ambientLight;
			RenderTexture.active = active;
			if (renderTexture != null)
			{
				RenderTexture.ReleaseTemporary(renderTexture);
			}
			if (gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(gameObject);
			}
			if (gameObject2 != null)
			{
				UnityEngine.Object.DestroyImmediate(gameObject2);
			}
		}
	}

	private static Color[] Draw(Camera camera, RenderTexture target, Color background)
	{
		camera.backgroundColor = background;
		camera.Render();
		RenderTexture.active = target;
		Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false);
		texture2D.ReadPixels(new Rect(0f, 0f, 128f, 128f), 0, 0);
		texture2D.Apply();
		Color[] pixels = texture2D.GetPixels();
		UnityEngine.Object.DestroyImmediate(texture2D);
		return pixels;
	}

	public static void Unload()
	{
		foreach (UnityEngine.Object item in s_made)
		{
			if (item != null)
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		s_made.Clear();
	}
}
