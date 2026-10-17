using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodFortified;

internal sealed class ChainTower : MonoBehaviour, Hoverable, Interactable, IPlaced
{
	public static readonly int IdKey = "CWChain_id".GetStableHashCode();

	public static readonly int PartnerKey = "CWChain_partner".GetStableHashCode();

	public static readonly int PartnerPosKey = "CWChain_partnerPos".GetStableHashCode();

	public static readonly int TargetKey = "CWChain_target".GetStableHashCode();

	public static readonly int FromKey = "CWChain_from".GetStableHashCode();

	public static readonly int SinceKey = "CWChain_since".GetStableHashCode();

	public static readonly int SecondsKey = "CWChain_seconds".GetStableHashCode();

	public static readonly int TravelKey = "CWChain_travel".GetStableHashCode();

	private const string WinchRpc = "CWChain_Winch";

	private const string UnlinkRpc = "CWChain_Unlink";

	public const int Toggle = 0;

	public const int Raise = 1;

	public const int Lower = 2;

	private const float DrumCircumference = 2.261947f;

	private const float UsualTravel = 7f;

	private const float ShortestRun = 2f;

	public static readonly List<ChainTower> All = new List<ChainTower>();

	private ZNetView m_nview;

	private WearNTear m_wear;

	private Transform m_drum;

	// The drum's rotation in the prefab, and the tower's sideways axis in the drum parent's space: the drum
	// winds around that axis on top of its own rotation.
	private Quaternion m_drumRest;

	private Vector3 m_drumAxis;

	private ChainNet m_net;

	private float m_checkTimer;

	private float m_partnerMissingFor;

	private LineRenderer m_preview;

	private static TextMeshProUGUI s_cursorLabel;

	// The arrow on the placement ghost at its "chain_direction" child, pointing out of the tower's front.
	private GameObject m_arrow;

	private static Mesh s_arrowMesh;

	private bool m_turning;

	private float m_lastHeight = -1f;

	private float m_lastTurned = -10f;

	private float m_lastStarted = -10f;

	private GameObject m_turningLoop;

	private ChainTower m_lastHolder;

	private static readonly Dictionary<AudioClip, float> s_quietStarts = new Dictionary<AudioClip, float>();

	private static readonly MethodInfo s_getData = typeof(AudioClip).GetMethod("GetData", new Type[2]
	{
		typeof(float[]),
		typeof(int)
	});

	public static string PreviewText = "";

	public static bool PreviewLinks;

	public ZNetView NView => m_nview;

	public WearNTear Wear => m_wear;

	public ChainNet Net => m_net;

	public bool Valid
	{
		get
		{
			if (m_nview != null)
			{
				return m_nview.IsValid();
			}
			return false;
		}
	}

	public long Id
	{
		get
		{
			if (!Valid)
			{
				return 0L;
			}
			return m_nview.GetZDO().GetLong(IdKey, 0L);
		}
	}

	public long PartnerId
	{
		get
		{
			if (!Valid)
			{
				return 0L;
			}
			return m_nview.GetZDO().GetLong(PartnerKey, 0L);
		}
	}

	// Where the net hangs: the prefab's "chain_attach" child, or the spot the original towers used.
	public Vector3 Port => base.transform.TransformPoint(m_port);

	private Vector3 m_port;

	private int m_prefab;

	// The longest net this tower's size can hold; two towers together hold the shorter of their two.
	public float MaxSpan => ChainSettings.MaxSpan(m_prefab);

	public float Target
	{
		get
		{
			if (!Valid)
			{
				return 0f;
			}
			return m_nview.GetZDO().GetFloat(TargetKey);
		}
	}

	public float Travel
	{
		get
		{
			if (!KnownTravel(out var travel))
			{
				return 7f;
			}
			return travel;
		}
	}

	private string Name
	{
		get
		{
			Piece component = GetComponent<Piece>();
			if ((object)component == null || string.IsNullOrEmpty(component.m_name))
			{
				return LocalisedText.Get("text_chain_tower");
			}
			return component.m_name;
		}
	}

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_wear = GetComponent<WearNTear>();
		m_prefab = Utils.GetPrefabName(base.gameObject).GetStableHashCode();
		m_drum = base.transform.Find("New/drum");
		if (m_drum != null)
		{
			m_drumRest = m_drum.localRotation;
			m_drumAxis = m_drum.parent.InverseTransformDirection(base.transform.right).normalized;
		}
		else if (!Player.IsPlacementGhost(base.gameObject))
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + Utils.GetPrefabName(base.gameObject) + " has no New/drum child, its winch drum will not turn.");
		}
		Transform port = base.transform.Find("chain_attach");
		m_port = (port != null) ? port.localPosition : new Vector3(0f, 2.5f - 0.35f, 1.28f);
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			All.Add(this);
			m_nview.Unregister("CWChain_Winch");
			m_nview.Unregister("CWChain_Unlink");
			m_nview.Register<int>("CWChain_Winch", RPC_Winch);
			m_nview.Register<long>("CWChain_Unlink", RPC_Unlink);
			if (m_wear != null)
			{
				WearNTear wear = m_wear;
				wear.m_onDestroyed = (Action)Delegate.Combine(wear.m_onDestroyed, new Action(OnDestroyedByDamage));
			}
			if (m_nview.IsOwner() && Id == 0L)
			{
				m_nview.GetZDO().Set(IdKey, NewId());
			}
			m_checkTimer = UnityEngine.Random.Range(0f, 0.3f);
		}
	}

	private void OnDisable()
	{
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			PreviewText = "";
			HideCursorLabel();
			if (m_preview != null)
			{
				m_preview.gameObject.SetActive(value: false);
			}
		}
	}

	private void OnDestroy()
	{
		All.Remove(this);
		DestroyNet();
		if (m_wear != null)
		{
			WearNTear wear = m_wear;
			wear.m_onDestroyed = (Action)Delegate.Remove(wear.m_onDestroyed, new Action(OnDestroyedByDamage));
		}
		if (m_nview != null && m_nview.GetZDO() != null)
		{
			m_nview.Unregister("CWChain_Winch");
			m_nview.Unregister("CWChain_Unlink");
		}
		if (m_preview != null)
		{
			UnityEngine.Object.Destroy(m_preview.gameObject);
			HideCursorLabel();
		}
	}

	private static long NewId()
	{
		long num;
		for (num = 0L; num == 0L; num = ((long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 32) ^ (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue))
		{
		}
		return num;
	}

	public void OnPlaced()
	{
		if (!Valid)
		{
			return;
		}
		if (Id == 0L)
		{
			m_nview.GetZDO().Set(IdKey, NewId());
		}
		float span;
		string why;
		ChainTower chainTower = FindPartner(Port, base.transform.forward, this, MaxSpan, out span, out why);
		Player localPlayer = Player.m_localPlayer;
		if (chainTower == null)
		{
			if (why.Length > 0)
			{
				localPlayer?.Message(MessageHud.MessageType.Center, why);
			}
			Jotunn.Logger.LogInfo(string.Format("tower {0:X} placed at {1}, not linked: {2}", Id, base.transform.position, (why.Length > 0) ? why : "no free tower near"));
			return;
		}
		ZDO zDO = m_nview.GetZDO();
		zDO.Set(PartnerKey, chainTower.Id);
		zDO.Set(PartnerPosKey, chainTower.transform.position);
		zDO.Set(TargetKey, 0f);
		zDO.Set(FromKey, 0f);
		zDO.Set(SinceKey, ZNet.instance.GetTime().Ticks);
		Jotunn.Logger.LogInfo($"tower {Id:X} placed at {base.transform.position}, linked to {chainTower.Id:X} at {chainTower.transform.position}: span {span:F1} m");
		m_checkTimer = 0f;
	}

	public static ChainTower FindPartner(Vector3 port, Vector3 forward, ChainTower self, float maxSpan, out float span, out string why)
	{
		ChainTower rejected;
		return FindPartner(port, forward, self, maxSpan, out span, out why, out rejected);
	}

	public static ChainTower FindPartner(Vector3 port, Vector3 forward, ChainTower self, float maxSpan, out float span, out string why, out ChainTower rejected)
	{
		ChainTower chainTower = null;
		rejected = null;
		span = 0f;
		float facingTolerance = ChainSettings.FacingTolerance;
		float minSpan = ChainSettings.MinSpan;
		why = "";
		float num = float.MaxValue;
		foreach (ChainTower item in All)
		{
			if (item == self || !item.Valid || item.IsLinked())
			{
				continue;
			}
			Vector3 vector = item.Port - port;
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
			float magnitude = vector2.magnitude;
			float num2 = Mathf.Min(maxSpan, item.MaxSpan);
			if (magnitude > num2 * 1.5f || magnitude < 0.5f)
			{
				continue;
			}
			Vector3 forward2 = item.transform.forward;
			string text = null;
			if (magnitude > num2)
			{
				text = LocalisedText.Get("text_chain_too_far", magnitude.ToString("F0"), num2.ToString("F0"));
			}
			else if (magnitude < minSpan)
			{
				text = LocalisedText.Get("text_chain_too_close", magnitude.ToString("F0"), minSpan.ToString("F0"));
			}
			else if (Vector3.Angle(new Vector3(forward.x, 0f, forward.z), vector2) > facingTolerance || Vector3.Angle(new Vector3(forward2.x, 0f, forward2.z), -vector2) > facingTolerance)
			{
				text = LocalisedText.Get("text_chain_must_face");
			}
			if (text != null)
			{
				if (magnitude < num)
				{
					num = magnitude;
					why = text;
					rejected = item;
				}
			}
			else if (!(chainTower != null) || !(magnitude >= span))
			{
				chainTower = item;
				span = magnitude;
			}
		}
		if (chainTower != null)
		{
			why = "";
			rejected = null;
		}
		return chainTower;
	}

	public bool IsLinked()
	{
		if (PartnerId != 0L)
		{
			return true;
		}
		return Master() != null;
	}

	public ChainTower Partner()
	{
		long partnerId = PartnerId;
		if (partnerId == 0L)
		{
			return null;
		}
		foreach (ChainTower item in All)
		{
			if (item != this && item.Valid && item.Id == partnerId)
			{
				return item;
			}
		}
		return null;
	}

	public ChainTower Master()
	{
		long id = Id;
		if (id == 0L)
		{
			return null;
		}
		foreach (ChainTower item in All)
		{
			if (item != this && item.Valid && item.PartnerId == id)
			{
				return item;
			}
		}
		return null;
	}

	public ChainTower StateHolder()
	{
		if (PartnerId == 0L)
		{
			return Master();
		}
		return this;
	}

	public float Height()
	{
		if (!Valid)
		{
			return 0f;
		}
		ZDO zDO = m_nview.GetZDO();
		float num = zDO.GetFloat(TargetKey);
		float num2 = zDO.GetFloat(FromKey);
		if (Mathf.Approximately(num, num2))
		{
			return num;
		}
		double num3 = (double)(ZNet.instance.GetTime().Ticks - zDO.GetLong(SinceKey, 0L)) / 10000000.0;
		return Mathf.MoveTowards(num2, num, (float)(num3 / (double)Duration(num > num2)));
	}

	public bool Moving()
	{
		if (!Valid)
		{
			return false;
		}
		if (m_net != null && Mathf.Abs(m_net.ShownHeight - Height()) > 0.0001f)
		{
			return true;
		}
		ZDO zDO = m_nview.GetZDO();
		float num = zDO.GetFloat(TargetKey);
		float num2 = zDO.GetFloat(FromKey);
		if (Mathf.Approximately(num, num2))
		{
			return false;
		}
		return (double)(ZNet.instance.GetTime().Ticks - zDO.GetLong(SinceKey, 0L)) / 10000000.0 < (double)(Duration(num > num2) * Mathf.Abs(num - num2)) + 0.5;
	}

	public float Duration(bool raising)
	{
		if (Valid)
		{
			ZDO zDO = m_nview.GetZDO();
			float num = zDO.GetFloat(SecondsKey);
			if (num > 0f && zDO.GetFloat(TargetKey) > 0.5f == raising)
			{
				return num;
			}
		}
		return RunSeconds(raising);
	}

	private float RunSeconds(bool raising)
	{
		float num = Mathf.Max(0.05f, raising ? ChainSettings.RaiseSpeed : ChainSettings.LowerSpeed);
		return Mathf.Max(2f, Travel / num);
	}

	private bool KnownTravel(out float travel)
	{
		travel = ((m_net != null) ? m_net.Travel : (Valid ? m_nview.GetZDO().GetFloat(TravelKey) : 0f));
		return travel > 0f;
	}

	public bool Command(int order)
	{
		ChainTower chainTower = StateHolder();
		if (chainTower == null || !chainTower.Valid)
		{
			return false;
		}
		chainTower.m_nview.InvokeRPC("CWChain_Winch", order);
		return true;
	}

	private void RPC_Winch(long sender, int order)
	{
		if (Valid && m_nview.IsOwner() && PartnerId != 0L)
		{
			float num = ((m_net != null) ? m_net.ShownHeight : Height());
			float travel = order switch
			{
				1 => 1f, 
				2 => 0f, 
				_ => (Target > 0.5f) ? 0f : 1f, 
			};
			float num2 = travel;
			float num3 = (KnownTravel(out travel) ? RunSeconds(num2 > 0.5f) : 0f);
			ZDO zDO = m_nview.GetZDO();
			zDO.Set(FromKey, num);
			zDO.Set(TargetKey, num2);
			zDO.Set(SinceKey, ZNet.instance.GetTime().Ticks);
			zDO.Set(SecondsKey, num3);
			Jotunn.Logger.LogInfo(string.Format("winch on {0:X}: {1} from {2:F2}, {3} (asked by {4})", Id, (num2 > 0.5f) ? "raising" : "lowering", num, (num3 > 0f) ? $"a whole run {num3:F1} s for {Travel:F1} m" : "the travel not known here: each client times it", sender));
		}
	}

	private void RPC_Unlink(long sender, long partner)
	{
		if (Valid && m_nview.IsOwner() && PartnerId == partner)
		{
			Unlink("the other tower is gone");
		}
	}

	private void Unlink(string why)
	{
		Jotunn.Logger.LogInfo($"tower {Id:X} unlinked from {PartnerId:X}: {why}");
		m_nview.GetZDO().Set(PartnerKey, 0L);
		DestroyNet();
	}

	private void OnDestroyedByDamage()
	{
		ChainTower chainTower = Master();
		if (chainTower != null && chainTower.Valid)
		{
			if (chainTower.m_nview.IsOwner())
			{
				chainTower.Unlink("the other tower was destroyed");
			}
			else
			{
				chainTower.m_nview.InvokeRPC("CWChain_Unlink", Id);
			}
		}
		DestroyNet();
	}

	private void Update()
	{
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			if (Player.IsPlacementGhost(base.gameObject))
			{
				UpdateGhost();
			}
		}
		else if (m_nview.IsValid())
		{
			m_checkTimer -= Time.deltaTime;
			if (m_checkTimer <= 0f)
			{
				m_checkTimer = 0.5f;
				CheckLink();
			}
			ChainTower chainTower = ((m_net != null) ? this : StateHolder());
			float num = ((!(chainTower != null)) ? 0f : ((chainTower.m_net != null) ? chainTower.m_net.ShownHeight : chainTower.Height()));
			float num2 = ((chainTower != null) ? (chainTower.Travel / 2.261947f) : 0f);
			if (m_drum != null)
			{
				m_drum.localRotation = Quaternion.AngleAxis((0f - num) * num2 * 360f, m_drumAxis) * m_drumRest;
			}
			if (chainTower != null && chainTower == m_lastHolder && m_lastHeight >= 0f && Mathf.Abs(num - m_lastHeight) > 1E-05f && chainTower.Moving())
			{
				m_lastTurned = Time.time;
			}
			m_lastHeight = num;
			m_lastHolder = chainTower;
			// Only the tower holding the net plays the winch, so a pair never doubles the sound.
			UpdateSound(chainTower == this && Time.time - m_lastTurned < 1f);
		}
	}

	private void UpdateSound(bool turning)
	{
		if (turning == m_turning)
		{
			return;
		}
		m_turning = turning;
		if (m_turningLoop != null)
		{
			UnityEngine.Object.Destroy(m_turningLoop);
		}
		m_turningLoop = null;
		Vector3 at = ((m_drum != null) ? m_drum.position : base.transform.position);
		if (!(Time.timeSinceLevelLoad < 5f) && turning)
		{
			if (Time.time - m_lastStarted > 3f)
			{
				Sound("sfx_frozenking_chainflurry_chain", at, 0.5f);
			}
			m_lastStarted = Time.time;
			// The mod's own winch loop, tuned in its Unity prefab; the game's gate loop if it is missing from the bundle.
			m_turningLoop = Sound("sfx_tower_chains_cwf", at, 1f, keep: true) ?? Sound("sfx_deepnorth_gate_close_loop", at, 0.45f, keep: true);
		}
	}

	// Plays a copy of a game sound prefab, so it keeps the game's mixer and falloff.
	private GameObject Sound(string prefab, Vector3 at, float volume, bool keep = false)
	{
		GameObject gameObject = ((ZNetScene.instance != null) ? ZNetScene.instance.GetPrefab(prefab) : null);
		if (gameObject == null)
		{
			return null;
		}
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject2;
		try
		{
			gameObject2 = UnityEngine.Object.Instantiate(gameObject, at, Quaternion.identity, base.transform);
		}
		finally
		{
			ZNetView.m_forceDisableInit = false;
		}
		ZSFX[] componentsInChildren = gameObject2.GetComponentsInChildren<ZSFX>();
		foreach (ZSFX obj in componentsInChildren)
		{
			obj.SetVolumeModifier(volume * MainConfig.chainTowerSoundVolume.Value);
			obj.m_minDelay = (obj.m_maxDelay = 0f);
			obj.m_fadeInDuration = Mathf.Min(obj.m_fadeInDuration, 0.05f);
			obj.m_playOnAwake = false;
			obj.Play();
			AudioSource component = obj.GetComponent<AudioSource>();
			if (component != null && component.clip != null && component.isPlaying)
			{
				component.time = QuietStart(component.clip);
			}
		}
		TimedDestruction component2 = gameObject2.GetComponent<TimedDestruction>();
		float t = ((component2 != null) ? component2.m_timeout : 10f);
		if (component2 != null)
		{
			UnityEngine.Object.Destroy(component2);
		}
		if (!keep)
		{
			UnityEngine.Object.Destroy(gameObject2, t);
		}
		return gameObject2;
	}

	internal static float QuietStart(AudioClip clip)
	{
		if (s_quietStarts.TryGetValue(clip, out var value))
		{
			return value;
		}
		value = 0f;
		try
		{
			// Only decompressed clips can be read; asking a compressed one logs a Unity error.
			if (clip.loadType == AudioClipLoadType.DecompressOnLoad && clip.samples > 0)
			{
				float[] array = new float[clip.samples * clip.channels];
				if (s_getData != null && (bool)s_getData.Invoke(clip, new object[2] { array, 0 }))
				{
					float peak = 0f;
					float[] array2 = array;
					foreach (float f in array2)
					{
						peak = Mathf.Max(peak, Mathf.Abs(f));
					}
					int num = ((peak > 0f) ? Array.FindIndex(array, (float v) => Mathf.Abs(v) >= peak * 0.1f) : (-1));
					if (num > 0)
					{
						value = Mathf.Min((float)num / (float)(clip.frequency * clip.channels), clip.length / 3f);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogDebug(clip.name + ": samples not read (" + ex.Message + ")");
		}
		s_quietStarts[clip] = value;
		Jotunn.Logger.LogInfo($"sound {clip.name}: {clip.length:F1} s, quiet for its first {value:F2} s");
		return value;
	}

	private void CheckLink()
	{
		if (PartnerId == 0L)
		{
			DestroyNet();
			return;
		}
		ChainTower chainTower = Partner();
		if (chainTower == null)
		{
			DestroyNet();
			Vector3 vec = m_nview.GetZDO().GetVec3(PartnerPosKey, base.transform.position);
			if (ZNet.instance != null && ZNetScene.InActiveArea(vec, ZNet.instance.GetReferencePosition()) && ZoneSystem.instance != null && ZoneSystem.instance.IsZoneLoaded(vec) && !TowerExists(vec, PartnerId))
			{
				m_partnerMissingFor += 0.5f;
			}
			else
			{
				m_partnerMissingFor = 0f;
			}
			if (m_partnerMissingFor > 15f && m_nview.IsOwner())
			{
				Unlink("the other tower is not there any more");
			}
			return;
		}
		m_partnerMissingFor = 0f;
		if (m_net == null)
		{
			m_net = ChainNet.Create(this, chainTower);
		}
		else if (!m_net.Fits(this, chainTower))
		{
			DestroyNet();
			m_net = ChainNet.Create(this, chainTower);
		}
		if (m_net != null && m_nview.IsOwner() && Mathf.Abs(m_nview.GetZDO().GetFloat(TravelKey) - m_net.Travel) > 0.05f)
		{
			m_nview.GetZDO().Set(TravelKey, m_net.Travel);
		}
	}

	private static bool TowerExists(Vector3 where, long id)
	{
		if (ZDOMan.instance == null)
		{
			return true;
		}
		List<ZDO> list = ZDOMan.instance.m_objectsBySector[ZoneSystem.SectorToIndex(ZoneSystem.GetZone(where)).Sector];
		if (list == null)
		{
			return false;
		}
		foreach (ZDO item in list)
		{
			if (item != null && ChainTowerPieces.IsTower(item.GetPrefab()) && item.GetLong(IdKey, 0L) == id)
			{
				return true;
			}
		}
		return false;
	}

	public void DestroyNet()
	{
		if (m_net != null)
		{
			UnityEngine.Object.Destroy(m_net.gameObject);
		}
		m_net = null;
	}

	private void UpdatePreview()
	{
		Vector3 port = Port;
		float span;
		string why;
		ChainTower rejected;
		ChainTower chainTower = FindPartner(port, base.transform.forward, null, MaxSpan, out span, out why, out rejected);
		ChainTower chainTower2 = chainTower ?? rejected;
		if (chainTower2 == null)
		{
			if (m_preview != null)
			{
				m_preview.gameObject.SetActive(value: false);
			}
			PreviewText = "";
			HideCursorLabel();
			return;
		}
		bool flag = chainTower != null;
		if (m_preview == null)
		{
			GameObject gameObject = new GameObject("CWChain_SpanPreview");
			m_preview = gameObject.AddComponent<LineRenderer>();
			m_preview.sharedMaterial = ChainMaterials.Overlay;
			m_preview.positionCount = 16;
			m_preview.widthMultiplier = 0.12f;
			m_preview.useWorldSpace = true;
			m_preview.shadowCastingMode = ShadowCastingMode.Off;
			m_preview.receiveShadows = false;
		}
		m_preview.gameObject.SetActive(value: true);
		Color color = (flag ? new Color(0.3f, 1f, 0.35f, 0.9f) : new Color(1f, 0.3f, 0.25f, 0.9f));
		m_preview.startColor = color;
		m_preview.endColor = color;
		Vector3 a = port;
		Vector3 port2 = chainTower2.Port;
		float num = Mathf.Min(1.5f, Vector3.Distance(a, port2) * 0.04f);
		for (int i = 0; i < 16; i++)
		{
			float num2 = (float)i / 15f;
			m_preview.SetPosition(i, Vector3.Lerp(a, port2, num2) + Vector3.down * (num * 4f * num2 * (1f - num2)));
		}
		PreviewLinks = flag;
		PreviewText = (flag ? LocalisedText.Get("text_chain_links_across", span.ToString("F0")) : why);
		Vector3 gap = port2 - a;
		ShowCursorLabel(LocalisedText.Get("text_metres", new Vector3(gap.x, 0f, gap.z).magnitude.ToString("F0")), color);
	}

	// The distance between the towers, shown on the HUD just above the build crosshair.
	private static void ShowCursorLabel(string text, Color color)
	{
		if (s_cursorLabel == null)
		{
			s_cursorLabel = CreateCursorLabel();
			if (s_cursorLabel == null)
			{
				return;
			}
		}
		s_cursorLabel.text = text;
		s_cursorLabel.color = color;
		s_cursorLabel.gameObject.SetActive(value: true);
	}

	private static void HideCursorLabel()
	{
		if (s_cursorLabel != null)
		{
			s_cursorLabel.gameObject.SetActive(value: false);
		}
	}

	// The arrow and the link preview, run once a frame from the tower's own Update and from the player's ghost
	// update. With some mod lists the ghost's own Update never runs, so the player's update keeps them working
	private int m_ghostFrame = -1;

	public void UpdateGhost()
	{
		if (m_ghostFrame == Time.frameCount)
		{
			return;
		}
		m_ghostFrame = Time.frameCount;
		UpdateDirectionArrow();
		UpdatePreview();
	}

	// Checked every frame rather than made once, so an arrow that could not be made yet, or was removed, comes back
	// without having to pick the piece again. It is laid flat each frame, so it stays level however the ghost tilts.
	private void UpdateDirectionArrow()
	{
		if (m_arrow == null && !CreateArrow())
		{
			return;
		}
		if (!m_arrow.activeSelf)
		{
			m_arrow.SetActive(value: true);
		}
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		if (forward.sqrMagnitude > 0.0001f)
		{
			m_arrow.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	private bool CreateArrow()
	{
		Transform anchor = base.transform.Find("chain_direction");
		if (anchor == null || ChainMaterials.Overlay == null)
		{
			return false;
		}
		m_arrow = new GameObject("CWChain_DirectionArrow");
		m_arrow.layer = base.gameObject.layer;
		m_arrow.transform.SetParent(anchor, worldPositionStays: false);
		m_arrow.AddComponent<MeshFilter>().sharedMesh = ArrowMesh();
		MeshRenderer renderer = m_arrow.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = ChainMaterials.Overlay;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		return true;
	}

	private static Mesh ArrowMesh()
	{
		if (s_arrowMesh == null)
		{
			s_arrowMesh = BuildArrowMesh();
		}
		return s_arrowMesh;
	}

	// A flat arrow lying on the ground plane, 2.5 m long and pointing along +Z from its origin.
	private static Mesh BuildArrowMesh()
	{
		const float shaftHalfWidth = 0.15f;
		const float shaftLength = 1.6f;
		const float headHalfWidth = 0.55f;
		const float length = 2.5f;
		Mesh mesh = new Mesh
		{
			name = "CWChain_DirectionArrow",
			vertices = new Vector3[7]
			{
				new Vector3(0f - shaftHalfWidth, 0f, 0f),
				new Vector3(shaftHalfWidth, 0f, 0f),
				new Vector3(shaftHalfWidth, 0f, shaftLength),
				new Vector3(0f - shaftHalfWidth, 0f, shaftLength),
				new Vector3(0f - headHalfWidth, 0f, shaftLength),
				new Vector3(headHalfWidth, 0f, shaftLength),
				new Vector3(0f, 0f, length)
			},
			triangles = new int[9] { 0, 3, 2, 0, 2, 1, 4, 6, 5 }
		};
		Color color = new Color(1f, 0.85f, 0.2f, 0.9f);
		mesh.colors = new Color[7] { color, color, color, color, color, color, color };
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private static TextMeshProUGUI CreateCursorLabel()
	{
		Hud hud = Hud.instance;
		if (hud == null || hud.m_crosshair == null || hud.m_pieceDescription == null)
		{
			return null;
		}
		RectTransform crosshair = hud.m_crosshair.rectTransform;
		GameObject gameObject = new GameObject("CWChain_SpanLabel", typeof(RectTransform));
		RectTransform rect = (RectTransform)gameObject.transform;
		rect.SetParent(crosshair.parent, worldPositionStays: false);
		rect.anchorMin = crosshair.anchorMin;
		rect.anchorMax = crosshair.anchorMax;
		rect.pivot = new Vector2(0.5f, 0f);
		rect.sizeDelta = new Vector2(300f, 40f);
		rect.anchoredPosition = crosshair.anchoredPosition + new Vector2(0f, 24f);
		TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
		label.font = hud.m_pieceDescription.font;
		label.fontSize = 24f;
		label.fontStyle = FontStyles.Bold;
		label.alignment = TextAlignmentOptions.Bottom;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.raycastTarget = false;
		label.outlineWidth = 0.2f;
		label.outlineColor = Color.black;
		return label;
	}

	public string GetHoverName()
	{
		return Name;
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	public string GetHoverText()
	{
		if (!Valid)
		{
			return "";
		}
		if (!MainConfig.enableChainTowers.Value)
		{
			return Localization.instance.Localize(Name + "\n$text_chain_towers_disabled");
		}
		ChainTower chainTower = StateHolder();
		if (chainTower == null)
		{
			return Localization.instance.Localize(Name + " $text_chain_not_linked");
		}
		float num = ((chainTower.m_net != null) ? chainTower.m_net.ShownHeight : chainTower.Height());
		float target = chainTower.Target;
		bool flag = target > 0.5f;
		string text;
		if (chainTower.m_net != null && chainTower.m_net.Blocked)
		{
			text = "$text_chain_ship_in_way";
		}
		else if (Mathf.Approximately(num, target))
		{
			text = (flag ? "$text_chain_raised" : "$text_chain_lowered");
		}
		else
		{
			int num2 = Mathf.CeilToInt(Mathf.Abs(target - num) * chainTower.Duration(target > num));
			text = LocalisedText.Get(flag ? "text_chain_raising" : "text_chain_lowering", num2.ToString());
		}
		string text2 = (flag ? "$text_chain_lower_net" : "$text_chain_raise_net");
		return Localization.instance.Localize(Name + " (" + text + ")\n[<color=yellow><b>$KEY_Use</b></color>] " + text2);
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || !Valid || !MainConfig.enableChainTowers.Value)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (StateHolder() == null)
		{
			user.Message(MessageHud.MessageType.Center, "$text_chain_not_linked_msg");
			return true;
		}
		Command(0);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
