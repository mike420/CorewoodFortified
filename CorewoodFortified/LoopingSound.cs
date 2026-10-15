using UnityEngine;

namespace CorewoodFortified;

internal static class LoopingSound
{
	// Builds a looping AudioSource from a sound prefab's first clip and its AudioSource settings, so it keeps the
	// game's mixer and falloff but can be started and stopped at will instead of living for the prefab's fixed lifetime.
	// volume is the prefab's own volume, to be scaled by the caller's setting
	public static AudioSource Create(GameObject soundPrefab, Transform parent, string name, out float volume)
	{
		volume = 0f;
		ZSFX sfx = soundPrefab ? soundPrefab.GetComponent<ZSFX>() : null;
		AudioSource template = soundPrefab ? soundPrefab.GetComponentInChildren<AudioSource>() : null;
		if (!sfx || !template || sfx.m_audioClips == null || sfx.m_audioClips.Length == 0)
		{
			return null;
		}
		GameObject holder = new GameObject(name);
		holder.transform.SetParent(parent, worldPositionStays: false);
		AudioSource sound = holder.AddComponent<AudioSource>();
		sound.clip = sfx.m_audioClips[0];
		sound.loop = true;
		sound.playOnAwake = false;
		sound.outputAudioMixerGroup = template.outputAudioMixerGroup;
		sound.spatialBlend = template.spatialBlend;
		sound.rolloffMode = template.rolloffMode;
		sound.minDistance = template.minDistance;
		sound.maxDistance = template.maxDistance;
		sound.dopplerLevel = template.dopplerLevel;
		sound.spread = template.spread;
		sound.priority = template.priority;
		// A new AudioSource gets full reverb by default; the prefab's sound is set up dry
		sound.reverbZoneMix = template.reverbZoneMix;
		sound.bypassReverbZones = template.bypassReverbZones;
		sound.bypassEffects = template.bypassEffects;
		sound.bypassListenerEffects = template.bypassListenerEffects;
		if (template.rolloffMode == AudioRolloffMode.Custom)
		{
			sound.SetCustomCurve(AudioSourceCurveType.CustomRolloff, template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
		}
		volume = template.volume * sfx.m_maxVol;
		return sound;
	}

	// Plays the sound at full volume, or fades it out over fadeOut seconds and stops it
	public static void Update(AudioSource sound, bool playing, float volume, float fadeOut)
	{
		if (!sound)
		{
			return;
		}
		if (playing)
		{
			sound.volume = volume;
			if (!sound.isPlaying)
			{
				sound.Play();
			}
			return;
		}
		if (!sound.isPlaying)
		{
			return;
		}
		sound.volume = Mathf.MoveTowards(sound.volume, 0f, volume * Time.deltaTime / fadeOut);
		if (sound.volume <= 0f)
		{
			sound.Stop();
		}
	}
}
