using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CorewoodFortified;

internal sealed class ChainNet : MonoBehaviour
{
	private enum Detail
	{
		Simulated,
		Held,
		Hidden
	}

	public static readonly List<ChainNet> All = new List<ChainNet>();

	private const float Slack = 0.008f;

	private const float StepTime = 1f / 30f;

	private const int Iterations = 3;

	private const float BarrierThickness = 0.8f;

	private const int MaxRows = 32;

	private const int MaxNodes = 1100;

	private ChainTower m_master;

	private ChainTower m_partner;

	private Vector3 m_portA;

	private Vector3 m_portB;

	private float m_width;

	private float m_water;

	private int m_columns;

	private int m_rows;

	private float[] m_ground;

	private float[] m_bottom;

	private float[] m_top;

	private float[] m_low;

	private Vector3[] m_rest;

	private Vector3[] m_pos;

	private Vector3[] m_prev;

	private Vector3[] m_held;

	private bool[] m_pinned;

	private float[] m_restHorizontal;

	private Quaternion[] m_vertical;

	private Quaternion[] m_horizontal;

	private Transform[] m_bones;

	private SkinnedMeshRenderer m_renderer;

	private Mesh m_mesh;

	private SkinnedMeshRenderer m_farRenderer;

	private Mesh m_farMesh;

	private int m_farStep = -1;

	private Rigidbody m_body;

	private BoxCollider[] m_boxes;

	private BoxCollider[] m_hitboxes;

	private BoxCollider[] m_climbBoxes;

	private bool m_climbable;

	private float[] m_segmentTop;

	private float[] m_segmentLeft;

	private float[] m_segmentRight;

	private float m_drop;

	private NetBarrier m_barrier;

	private float m_height;

	private float m_blockedUntil;

	private float m_simClock;

	private float m_time;

	private Detail m_detail;

	private bool m_posed;

	private static readonly int s_vehicleMask = LayerMask.GetMask("vehicle");

	private const float LinksWithin = 45f;

	private const float FarWidth = 0.09f;

	private const float FarWidthPerMetre = 0.0016f;

	private const float FarStep = 1.25f;

	private const float ClimbReach = 3f;

	public static long CostTicks;

	public static long CostFrames;

	public float ShownHeight => m_height;

	public bool Blocked { get; private set; }

	public ChainTower Master => m_master;

	public ChainTower Partner => m_partner;

	public float Span => m_width;

	public int Columns => m_columns;

	public int Rows => m_rows;

	public int Links { get; private set; }

	public int Segments => m_boxes.Length;

	public float Travel { get; private set; }

	public string DetailName => m_detail.ToString();

	public float BarrierTop
	{
		get
		{
			BarrierTops(out var lowest, out var _);
			return lowest;
		}
	}

	public float WaterLevel => m_water;

	public static ChainNet Create(ChainTower master, ChainTower partner)
	{
		GameObject gameObject = new GameObject("CWChain_Net");
		ChainNet chainNet = gameObject.AddComponent<ChainNet>();
		if (chainNet.Setup(master, partner))
		{
			return chainNet;
		}
		UnityEngine.Object.Destroy(gameObject);
		return null;
	}

	public bool Fits(ChainTower master, ChainTower partner)
	{
		if (master == m_master && partner == m_partner && (master.Port - m_portA).sqrMagnitude < 0.01f)
		{
			return (partner.Port - m_portB).sqrMagnitude < 0.01f;
		}
		return false;
	}

	private bool Setup(ChainTower master, ChainTower partner)
	{
		m_master = master;
		m_partner = partner;
		m_portA = master.Port;
		m_portB = partner.Port;
		Vector3 vector = m_portB - m_portA;
		vector.y = 0f;
		m_width = vector.magnitude;
		if (m_width < 1f)
		{
			return false;
		}
		vector /= m_width;
		base.transform.SetPositionAndRotation(m_portA, Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), Vector3.up));
		m_water = ((ZoneSystem.instance != null) ? ZoneSystem.instance.m_waterLevel : 30f);
		float meshSpacing = ChainSettings.MeshSpacing;
		int num = Mathf.Clamp(Mathf.RoundToInt(m_width / meshSpacing), 2, 64);
		m_columns = num + 1;
		m_ground = new float[m_columns];
		m_bottom = new float[m_columns];
		m_top = new float[m_columns];
		m_low = new float[m_columns];
		float num2 = Mathf.Min(1.5f, m_width * 0.035f);
		float b = m_water + ChainSettings.RaisedClearance;
		float num3 = 0f;
		for (int i = 0; i < m_columns; i++)
		{
			float num4 = (float)i / (float)num;
			float num5 = Ground(m_portA + vector * (m_width * num4));
			float num6 = Mathf.Lerp(m_portA.y, m_portB.y, num4);
			float a = ((i == 0) ? m_portA.y : ((i == num) ? m_portB.y : Mathf.Max(num6 - num2 * 4f * num4 * (1f - num4), Mathf.Min(num6, b))));
			a = Mathf.Max(a, num5 + 0.3f);
			float num7 = Mathf.Min(a - 0.3f, num5 + 0.1f);
			float num8 = ((i == 0 || i == num) ? a : Mathf.Min(a, num7 + 0.25f));
			m_ground[i] = num5 - m_portA.y;
			m_top[i] = a - m_portA.y;
			m_bottom[i] = num7 - m_portA.y;
			m_low[i] = num8 - m_portA.y;
			num3 = Mathf.Max(num3, a - num7);
			Travel = Mathf.Max(Travel, a - num8);
		}
		int max = Mathf.Max(2, Mathf.Min(32, 1100 / m_columns - 1));
		m_rows = Mathf.Clamp(Mathf.CeilToInt(num3 / meshSpacing - 0.25f), 2, max) + 1;
		int num9 = m_columns * m_rows;
		m_rest = new Vector3[num9];
		m_pos = new Vector3[num9];
		m_prev = new Vector3[num9];
		m_held = new Vector3[num9];
		m_pinned = new bool[num9];
		m_vertical = new Quaternion[num9];
		m_horizontal = new Quaternion[num9];
		m_restHorizontal = new float[num9];
		int num10 = m_rows - 1;
		for (int j = 0; j < m_columns; j++)
		{
			float x = m_width * (float)j / (float)num;
			for (int k = 0; k < m_rows; k++)
			{
				int num11 = j * m_rows + k;
				m_rest[num11] = new Vector3(x, Mathf.Lerp(m_bottom[j], m_top[j], (float)k / (float)num10), 0f);
				m_pinned[num11] = k == 0 || k == num10 || j == 0 || j == num;
				m_vertical[num11] = (m_horizontal[num11] = Quaternion.identity);
			}
		}
		for (int l = 0; l + 1 < m_columns; l++)
		{
			for (int m = 0; m < m_rows; m++)
			{
				m_restHorizontal[l * m_rows + m] = Vector3.Distance(m_rest[l * m_rows + m], m_rest[(l + 1) * m_rows + m]) * 1.008f;
			}
		}
		BuildMesh();
		BuildBarrier(vector);
		m_height = m_master.Height();
		if (m_height > 0f && ShipInTheWay(0f, m_height))
		{
			m_height = 0f;
		}
		HeldPose(m_pos);
		Array.Copy(m_pos, m_prev, num9);
		WriteBones();
		PlaceBarrier(teleport: true);
		All.Add(this);
		Jotunn.Logger.LogInfo($"net {m_master.Id:X} -> {m_partner.Id:X}: span {m_width:F1} m, {m_columns}x{m_rows} nodes, {Links} links, {m_mesh.vertexCount} vertices, height {m_height:F2}, top chain {m_portA.y + Min(m_top):F1}..{m_portA.y + Max(m_top):F1}, bottom {m_portA.y + Min(m_bottom):F1}..{m_portA.y + Max(m_bottom):F1}, the winch moves it {Travel:F1} m, barrier sinks {m_drop:F1} m");
		return true;
	}

	private static float Ground(Vector3 at)
	{
		return ZoneSystem.instance.GetGroundHeight(at);
	}

	private void OnDestroy()
	{
		All.Remove(this);
		if (m_mesh != null)
		{
			UnityEngine.Object.Destroy(m_mesh);
		}
		if (m_farMesh != null)
		{
			UnityEngine.Object.Destroy(m_farMesh);
		}
	}

	private void BuildMesh()
	{
		int num = m_rest.Length;
		m_bones = new Transform[num * 2];
		for (int i = 0; i < num * 2; i++)
		{
			Transform transform = new GameObject(((i < num) ? "v" : "h") + i % num).transform;
			transform.SetParent(base.transform, worldPositionStays: false);
			transform.localPosition = m_rest[i % num];
			m_bones[i] = transform;
		}
		m_mesh = NetMesh.Build(m_rest, m_columns, m_rows, out var links);
		Links = links;
		GameObject gameObject = new GameObject("links");
		gameObject.layer = LayerMask.NameToLayer("piece");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		m_renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
		m_renderer.sharedMesh = m_mesh;
		m_renderer.bones = m_bones;
		m_renderer.rootBone = base.transform;
		m_renderer.sharedMaterial = ChainMaterials.Chain;
		m_renderer.updateWhenOffscreen = false;
		float num2 = Mathf.Min(Min(m_ground), Min(m_bottom)) - 2f;
		float num3 = Max(m_top) + 1.5f;
		m_renderer.localBounds = new Bounds(new Vector3(m_width * 0.5f, (num2 + num3) * 0.5f, 0f), new Vector3(m_width + 2f, num3 - num2, 6f));
		GameObject gameObject2 = new GameObject("links_far");
		gameObject2.layer = gameObject.layer;
		gameObject2.transform.SetParent(base.transform, worldPositionStays: false);
		m_farRenderer = gameObject2.AddComponent<SkinnedMeshRenderer>();
		m_farRenderer.bones = m_bones;
		m_farRenderer.rootBone = base.transform;
		m_farRenderer.sharedMaterial = ChainMaterials.Chain;
		m_farRenderer.updateWhenOffscreen = false;
		m_farRenderer.localBounds = m_renderer.localBounds;
		m_farRenderer.enabled = false;
	}

	private void BuildBarrier(Vector3 along)
	{
		int num = m_columns - 1;
		m_boxes = new BoxCollider[num];
		m_hitboxes = new BoxCollider[num];
		m_climbBoxes = new BoxCollider[num];
		m_segmentTop = new float[num];
		m_segmentLeft = new float[num];
		m_segmentRight = new float[num];
		GameObject gameObject = new GameObject("barrier");
		gameObject.layer = LayerMask.NameToLayer("blocker");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		m_body = gameObject.AddComponent<Rigidbody>();
		m_body.isKinematic = true;
		m_body.useGravity = false;
		m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		int layer = LayerMask.NameToLayer("blocker");
		int layer2 = LayerMask.NameToLayer("hitbox");
		int layer3 = LayerMask.NameToLayer("static_solid");
		m_drop = 0f;
		for (int i = 0; i < num; i++)
		{
			float num2 = m_width * (float)i / (float)num;
			float num3 = m_width * (float)(i + 1) / (float)num;
			float num4 = Mathf.Min(Ground(m_portA + along * ((num2 + num3) * 0.5f)) - m_portA.y, Mathf.Min(m_ground[i], m_ground[i + 1]));
			float num5 = Mathf.Min(m_top[i], m_top[i + 1]);
			float num6 = num4 - 1.5f;
			m_segmentTop[i] = num5;
			m_segmentLeft[i] = num2;
			m_segmentRight[i] = num3;
			m_drop = Mathf.Max(m_drop, num5 - (num4 - 0.6f));
			GameObject gameObject2 = new GameObject("box" + i);
			gameObject2.layer = layer;
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			gameObject2.transform.localPosition = new Vector3((num2 + num3) * 0.5f, (num5 + num6) * 0.5f, 0f);
			BoxCollider boxCollider = gameObject2.AddComponent<BoxCollider>();
			boxCollider.size = new Vector3(num3 - num2 + 0.1f, num5 - num6, 0.8f);
			m_boxes[i] = boxCollider;
			GameObject gameObject3 = new GameObject("hitbox");
			gameObject3.layer = layer2;
			gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			m_hitboxes[i] = gameObject3.AddComponent<BoxCollider>();
			m_hitboxes[i].size = boxCollider.size;
			GameObject gameObject4 = new GameObject("climb");
			gameObject4.layer = layer3;
			gameObject4.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			m_climbBoxes[i] = gameObject4.AddComponent<BoxCollider>();
			m_climbBoxes[i].size = boxCollider.size;
			m_climbBoxes[i].enabled = false;
		}
		m_drop = Mathf.Max(0f, m_drop);
		m_barrier = gameObject.AddComponent<NetBarrier>();
		m_barrier.Net = this;
	}

	private Vector3 BarrierOffset()
	{
		return new Vector3(0f, (0f - (1f - m_height)) * m_drop, 0f);
	}

	private void PlaceBarrier(bool teleport)
	{
		Vector3 position = base.transform.TransformPoint(BarrierOffset());
		if (teleport || !m_body.gameObject.activeInHierarchy)
		{
			m_body.transform.position = position;
			m_body.position = position;
		}
		else
		{
			m_body.MovePosition(position);
		}
	}

	private void FixedUpdate()
	{
		if (m_master == null || m_partner == null)
		{
			return;
		}
		float num = m_master.Height();
		float fixedDeltaTime = Time.fixedDeltaTime;
		Blocked = false;
		if (num > m_height + 0.0001f)
		{
			float num2 = Mathf.MoveTowards(m_height, num, fixedDeltaTime * 2f / m_master.Duration(raising: true));
			if (Time.time < m_blockedUntil || ShipInTheWay(m_height, num2))
			{
				if (Time.time >= m_blockedUntil)
				{
					m_blockedUntil = Time.time + 1f;
				}
				Blocked = true;
			}
			else
			{
				m_height = num2;
			}
		}
		else if (num < m_height - 0.0001f)
		{
			m_height = Mathf.MoveTowards(m_height, num, fixedDeltaTime * 2f / m_master.Duration(raising: false));
		}
		else
		{
			m_height = num;
		}
		PlaceBarrier(teleport: false);
	}

	private bool ShipInTheWay(float from, float to)
	{
		float num = m_water - m_portA.y;
		for (int i = 0; i < m_boxes.Length; i++)
		{
			float num2 = m_segmentTop[i] - (1f - from) * m_drop;
			float num3 = m_segmentTop[i] - (1f - to) * m_drop;
			if (!(num3 < num - 3f))
			{
				Vector3 center = base.transform.TransformPoint(new Vector3((m_segmentLeft[i] + m_segmentRight[i]) * 0.5f, (num3 + num2) * 0.5f, 0f));
				Vector3 halfExtents = new Vector3((m_segmentRight[i] - m_segmentLeft[i]) * 0.5f, Mathf.Max(0.05f, (num3 - num2) * 0.5f + 0.1f), 0.5f);
				if (Physics.CheckBox(center, halfExtents, base.transform.rotation, s_vehicleMask, QueryTriggerInteraction.Ignore))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void Update()
	{
		if (m_master == null || m_partner == null)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		float num = ((mainCamera != null) ? DistanceToSpan(mainCamera.transform.position) : float.MaxValue);
		Detail detail = ((num > ChainSettings.DrawDistance) ? Detail.Hidden : ((num > ChainSettings.SimulationDistance) ? Detail.Held : Detail.Simulated));
		if (detail != m_detail)
		{
			if (detail == Detail.Simulated)
			{
				Array.Copy(m_pos, m_prev, m_pos.Length);
			}
			m_detail = detail;
			m_posed = false;
		}
		bool flag = detail != Detail.Hidden && num > 45f;
		if (flag)
		{
			UpdateFarMesh(num);
		}
		m_renderer.enabled = detail != Detail.Hidden && !flag;
		m_farRenderer.enabled = flag;
		UpdateClimbable();
	}

	private void UpdateFarMesh(float distance)
	{
		int num = Mathf.CeilToInt(Mathf.Log(Mathf.Max(0.09f, distance * 0.0016f) / 0.09f, 1.25f) - 0.001f);
		if (num != m_farStep)
		{
			m_farStep = num;
			if (m_farMesh != null)
			{
				UnityEngine.Object.Destroy(m_farMesh);
			}
			m_farMesh = NetMesh.BuildFar(m_rest, m_columns, m_rows, 0.09f * Mathf.Pow(1.25f, num));
			m_farRenderer.sharedMesh = m_farMesh;
		}
	}

	private void UpdateClimbable()
	{
		Player localPlayer = Player.m_localPlayer;
		bool flag = false;
		if (localPlayer != null && m_height > 0.02f)
		{
			Vector3 vector = base.transform.InverseTransformPoint(localPlayer.transform.position);
			flag = Mathf.Abs(vector.z) < 3f && vector.x > -3f && vector.x < m_width + 3f;
		}
		if (flag != m_climbable)
		{
			m_climbable = flag;
			BoxCollider[] climbBoxes = m_climbBoxes;
			for (int i = 0; i < climbBoxes.Length; i++)
			{
				climbBoxes[i].enabled = flag;
			}
		}
	}

	private void LateUpdate()
	{
		if (!(m_master == null) && !(m_partner == null))
		{
			long timestamp = Stopwatch.GetTimestamp();
			UpdateLooks();
			CostTicks += Stopwatch.GetTimestamp() - timestamp;
			CostFrames++;
		}
	}

	private void UpdateLooks()
	{
		float num = Mathf.Min(Time.deltaTime, 0.1f);
		m_time += num;
		switch (m_detail)
		{
		case Detail.Hidden:
			break;
		case Detail.Held:
		{
			HeldPose(m_held);
			float num3 = 0f;
			float t = 1f - Mathf.Exp((0f - num) * 3f);
			for (int i = 0; i < m_pos.Length; i++)
			{
				Vector3 vector = Vector3.Lerp(m_pos[i], m_held[i], t);
				num3 = Mathf.Max(num3, (vector - m_pos[i]).sqrMagnitude);
				m_pos[i] = vector;
				m_prev[i] = vector;
			}
			if (num3 > 1E-06f || !m_posed)
			{
				WriteBones();
				m_posed = num3 <= 1E-06f;
			}
			break;
		}
		default:
		{
			m_simClock += num;
			int num2 = 0;
			while (m_simClock >= 1f / 30f && num2 < 3)
			{
				m_simClock -= 1f / 30f;
				Step(1f / 30f);
				num2++;
			}
			if (m_simClock > 1f / 30f)
			{
				m_simClock = 0f;
			}
			if (num2 > 0)
			{
				WriteBones();
			}
			break;
		}
		}
	}

	private float TopNow(int i)
	{
		return Mathf.Lerp(m_low[i], m_top[i], m_height);
	}

	private float BottomNow(int i)
	{
		return m_bottom[i];
	}

	private void HeldPose(Vector3[] into)
	{
		int num = m_rows - 1;
		for (int i = 0; i < m_columns; i++)
		{
			float b = TopNow(i);
			float a = BottomNow(i);
			for (int j = 0; j < m_rows; j++)
			{
				into[i * m_rows + j] = new Vector3(m_rest[i * m_rows + j].x, Mathf.Lerp(a, b, (float)j / (float)num), 0f);
			}
		}
	}

	private void Step(float dt)
	{
		int num = m_rows - 1;
		int num2 = m_columns - 1;
		float num3 = m_water - m_portA.y;
		float num4 = dt * dt;
		for (int i = 1; i < num2; i++)
		{
			int num5 = i * m_rows + num;
			m_pos[num5] = new Vector3(m_rest[num5].x, TopNow(i), 0f);
		}
		for (int j = 0; j < m_pos.Length; j++)
		{
			if (!m_pinned[j])
			{
				Vector3 vector = m_pos[j];
				bool num6 = vector.y < num3;
				float num7 = (num6 ? 0.88f : 0.95f);
				float y = (num6 ? (-8.5347f) : (-9.81f));
				float z = (num6 ? (0.12f * Mathf.Sin(m_time * 0.5f + vector.x * 0.3f)) : 0f);
				Vector3 vector2 = (vector - m_prev[j]) * num7;
				m_prev[j] = vector;
				m_pos[j] = vector + vector2 + new Vector3(0f, y, z) * num4;
			}
		}
		for (int k = 0; k < 3; k++)
		{
			for (int l = 0; l < m_columns; l++)
			{
				float length = Mathf.Max(0.01f, TopNow(l) - BottomNow(l)) / (float)num * 1.008f;
				for (int m = 0; m < num; m++)
				{
					Constrain(l * m_rows + m, l * m_rows + m + 1, length);
				}
			}
			for (int n = 0; n + 1 < m_columns; n++)
			{
				for (int num8 = 1; num8 < num; num8++)
				{
					Constrain(n * m_rows + num8, (n + 1) * m_rows + num8, m_restHorizontal[n * m_rows + num8]);
				}
			}
			for (int num9 = 1; num9 + 1 < m_columns; num9++)
			{
				float num10 = m_ground[num9] + 0.08f;
				for (int num11 = 1; num11 < num; num11++)
				{
					int num12 = num9 * m_rows + num11;
					if (m_pos[num12].y < num10)
					{
						m_pos[num12].y = num10;
					}
				}
			}
		}
	}

	private void Constrain(int a, int b, float length)
	{
		Vector3 vector = m_pos[b] - m_pos[a];
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude <= length * length)
		{
			return;
		}
		bool flag = m_pinned[a];
		bool flag2 = m_pinned[b];
		if (!(flag && flag2))
		{
			float num = Mathf.Sqrt(sqrMagnitude);
			Vector3 vector2 = vector * ((num - length) / num);
			if (flag)
			{
				m_pos[b] -= vector2;
				return;
			}
			if (flag2)
			{
				m_pos[a] += vector2;
				return;
			}
			vector2 *= 0.5f;
			m_pos[a] += vector2;
			m_pos[b] -= vector2;
		}
	}

	private void WriteBones()
	{
		int b = m_rows - 1;
		int b2 = m_columns - 1;
		int num = m_pos.Length;
		for (int i = 0; i < m_columns; i++)
		{
			for (int j = 0; j < m_rows; j++)
			{
				int num2 = i * m_rows + j;
				Vector3 vector = m_pos[i * m_rows + Mathf.Min(j + 1, b)] - m_pos[i * m_rows + Mathf.Max(j - 1, 0)];
				if (vector.sqrMagnitude > 1E-08f)
				{
					vector.Normalize();
					Vector3 forward = Vector3.forward - vector * vector.z;
					if (forward.sqrMagnitude > 0.0001f)
					{
						m_vertical[num2] = Quaternion.LookRotation(forward, vector);
					}
				}
				Vector3 vector2 = m_pos[Mathf.Min(i + 1, b2) * m_rows + j] - m_pos[Mathf.Max(i - 1, 0) * m_rows + j];
				if (vector2.sqrMagnitude > 1E-08f)
				{
					vector2.Normalize();
					Vector3 vector3 = Vector3.up - vector2 * vector2.y;
					if (vector3.sqrMagnitude > 0.0001f)
					{
						m_horizontal[num2] = Quaternion.LookRotation(Vector3.Cross(vector2, vector3), vector3);
					}
				}
				m_bones[num2].SetLocalPositionAndRotation(m_pos[num2], m_vertical[num2]);
				m_bones[num + num2].SetLocalPositionAndRotation(m_pos[num2], m_horizontal[num2]);
			}
		}
	}

	public void TakeHit(HitData hit)
	{
		if (ChainSettings.Invulnerable)
		{
			return;
		}
		float damageToTowers = ChainSettings.DamageToTowers;
		if (damageToTowers <= 0f)
		{
			return;
		}
		ChainTower[] array = new ChainTower[2] { m_master, m_partner };
		foreach (ChainTower chainTower in array)
		{
			if (!(chainTower == null) && !(chainTower.Wear == null) && chainTower.Valid)
			{
				HitData hitData = hit.Clone();
				hitData.m_damage.Modify(damageToTowers);
				chainTower.Wear.Damage(hitData);
			}
		}
	}

	public float DistanceToSpan(Vector3 point)
	{
		Vector3 portA = m_portA;
		Vector3 vector = m_portB - portA;
		float num = Mathf.Clamp01(Vector3.Dot(point - portA, vector) / Mathf.Max(0.0001f, vector.sqrMagnitude));
		return Vector3.Distance(point, portA + vector * num);
	}

	public void BarrierTops(out float lowest, out float highest)
	{
		float y = BarrierOffset().y;
		lowest = m_portA.y + Min(m_segmentTop) + y;
		highest = m_portA.y + Max(m_segmentTop) + y;
	}

	public Vector3 NodeWorld(int column, int row)
	{
		return base.transform.TransformPoint(m_pos[column * m_rows + row]);
	}

	public void Sway(out float largest, out float middle)
	{
		largest = 0f;
		for (int i = 0; i < m_pos.Length; i++)
		{
			if (!m_pinned[i])
			{
				largest = Mathf.Max(largest, Mathf.Abs(m_pos[i].z));
			}
		}
		middle = m_pos[m_columns / 2 * m_rows + m_rows / 2].z;
	}

	public float LargestTwist()
	{
		float num = 0f;
		for (int i = 0; i < m_horizontal.Length; i++)
		{
			num = Mathf.Max(num, Vector3.Angle(m_horizontal[i] * Vector3.up, Vector3.up));
		}
		return num;
	}

	private static float Min(float[] values)
	{
		float num = float.MaxValue;
		foreach (float b in values)
		{
			num = Mathf.Min(num, b);
		}
		return num;
	}

	private static float Max(float[] values)
	{
		float num = float.MinValue;
		foreach (float b in values)
		{
			num = Mathf.Max(num, b);
		}
		return num;
	}
}
