using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// GOAPAction that enables an AI agent to break line‑of‑sight with the player by
/// ducking behind world geometry, waiting a short time, and then resuming its
/// stalk‑and‑ambush loop.  The behaviour is split into three internal phases
/// that match the design slide:
/// 
///     • Stalking  – actively circle the player while trying to stay outside of
///                   their hard FOV.
///     • Hiding    – sprint to the closest point that blocks the player’s view.
///     • Freezing  – stay perfectly still for a random interval before falling
///                   back to Stalking (action completes).
/// 
/// The action integrates with existing GOAPAgent.MoveTo() and relies on the
/// director‑driven belief "playerSpotted" to decide when it has been seen.
/// </summary>
public class HideFromPlayerAction : GOAPAction
{
	private enum Phase { Stalking, Hiding, Freezing }

	// ------------ Designer‑tweakable knobs --------------------------
	[Header("FOV Settings")]
	[Tooltip("Degrees of the hard (centre) FOV cone.  If the agent is inside " +
			 "this cone and un‑occluded, it is considered SEEN and will hide.")]
	public float hardFOV = 60f;
	[Tooltip("Degrees of the soft (peripheral) FOV cone.  When inside this cone " +
			 "but not hardFOV, the agent freezes.")]
	public float softFOV = 120f;
	[Tooltip("Maximum distance at which the player can spot the agent.")]
	public float detectionRadius = 25f;

	[Header("Cover Search Settings")]
	public float coverSearchRadius = 12f;
	public float coverSampleSpacing = 1.5f;   // metres between random samples
	public int maxCoverSamples = 32;     // attempts per query
	public LayerMask coverObstacles;          // world geometry used as cover

	[Header("Retreat / Freeze Settings")]
	public float minFreezeTime = 1.5f;
	public float maxFreezeTime = 3.5f;

	// ----------------------------------------------------------------
	private Phase _phase;
	private Transform _player;
	private GOAPAgent _goap;

	private Vector3 _cachedCoverPos;
	private bool _hasCover;
	private float _freezeTimer;
	private bool _completed;

	// ----------------------------------------------------------------
	private void Start()
	{
		GameObject pObj = GameObject.FindGameObjectWithTag("Player");
		if (pObj) _player = pObj.transform;
		_goap = GetComponent<GOAPAgent>();
	}

	public override bool IsDone() => _completed;
	public override bool RequiresInRange() => false;
	public override bool CheckProceduralPrecondition(GameObject agent)
	{
		return _player != null && _goap != null;
	}

	public override bool Perform(GameObject agentObj)
	{
		if (_player == null || _goap == null) return false;

		switch (_phase)
		{
			case Phase.Stalking: DoStalking(); break;
			case Phase.Hiding: DoHiding(); break;
			case Phase.Freezing: DoFreezing(); break;
		}
		return true;
	}

	// ------------------------------ PHASE LOGIC ---------------------
	private void DoStalking()
	{
		if (PlayerSeesAgent())
		{
			// Immediately break LoS – pick a cover point and dash.
			if (FindCover(out _cachedCoverPos))
			{
				_goap.MoveTo(_cachedCoverPos);
				_phase = Phase.Hiding;
				_hasCover = true;
			}
			else
			{
				// Fallback: just sprint directly away from the player.
				Vector3 fleeDir = (transform.position - _player.position).normalized;
				_goap.MoveTo(transform.position + fleeDir * 4f);
				_phase = Phase.Hiding;
			}
			return;
		}

		// Player does NOT see us – circle behind them.
		Vector3 behind = _player.position - _player.forward * 3f; // 3 m behind
		_goap.MoveTo(behind);

		// If we enter the soft FOV (but not seen), freeze briefly to build
		// tension.
		if (InsideSoftFOV() && !InsideHardFOV())
		{
			_phase = Phase.Freezing;
			_freezeTimer = Random.Range(minFreezeTime, maxFreezeTime);
			return;
		}
	}

	private void DoHiding()
	{
		if (!_hasCover)
		{
			_phase = Phase.Stalking;
			return;
		}

		float dist = Vector3.Distance(transform.position, _cachedCoverPos);
		bool reached = dist < 0.6f;
		bool losBlocked = !CanPlayerSeePoint(transform.position);

		if (reached && losBlocked)
		{
			// Start freeze timer once safely hidden.
			_phase = Phase.Freezing;
			_freezeTimer = Random.Range(minFreezeTime, maxFreezeTime);
		}
		else
		{
			// Continue travelling toward cover.
			_goap.MoveTo(_cachedCoverPos);
		}
	}

	private void DoFreezing()
	{
		_freezeTimer -= Time.deltaTime;
		if (_freezeTimer <= 0f)
		{
			_completed = true; // Let GOAP planner choose next action (likely Follow)
		}
	}

	// ------------------------------ HELPERS -------------------------
	private bool PlayerSeesAgent()
	{
		if (!InsideHardFOV()) return false;
		return CanPlayerSeePoint(transform.position);
	}

	private bool InsideHardFOV()
	{
		Vector3 toAgent = transform.position - _player.position;
		if (toAgent.sqrMagnitude > detectionRadius * detectionRadius) return false;
		float angle = Vector3.Angle(_player.forward, toAgent);
		return angle <= hardFOV * 0.5f;
	}

	private bool InsideSoftFOV()
	{
		Vector3 toAgent = transform.position - _player.position;
		if (toAgent.sqrMagnitude > detectionRadius * detectionRadius) return false;
		float angle = Vector3.Angle(_player.forward, toAgent);
		return angle <= softFOV * 0.5f;
	}

	private bool CanPlayerSeePoint(Vector3 worldPos)
	{
		Vector3 eye = _player.position + Vector3.up * 1.6f; // approximate eyes
		Vector3 dir = (worldPos + Vector3.up * 1f) - eye;
		float dist = dir.magnitude;
		if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, coverObstacles))
		{
			// Something blocks the ray before it reaches us.
			return false;
		}
		return true;
	}

	/// <summary>
	/// Samples random points on the NavMesh around the agent until it finds one
	/// that is occluded from the player.
	/// </summary>
	private bool FindCover(out Vector3 bestPos)
	{
		bestPos = Vector3.zero;
		int attempts = 0;
		float bestDist = Mathf.Infinity;

		while (attempts < maxCoverSamples)
		{
			attempts++;
			Vector3 randomDir = Random.insideUnitSphere * coverSearchRadius;
			randomDir.y = 0f;
			Vector3 candidate = transform.position + randomDir;
			if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1.2f, NavMesh.AllAreas))
				continue;

			if (!CanPlayerSeePoint(navHit.position))
			{
				float d = Vector3.Distance(transform.position, navHit.position);
				if (d < bestDist)
				{
					bestDist = d;
					bestPos = navHit.position;
				}
			}
		}
		return bestDist < Mathf.Infinity;
	}

	// ----------------------------------------------------------------
	// Public convenience for planners that wish to query if the agent is
	// currently attempting to hide.
	public bool IsHiding() => _phase == Phase.Hiding || _phase == Phase.Freezing;
}
