using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Tanks.Complete
{
    /// <summary>
    /// Computer opponent that plays like a person rather than a turret. It runs in three layers:
    /// <list type="bullet">
    /// <item>Perception: sees the enemy with a reaction delay, checks real line of sight against the level
    /// colliders, notices when the enemy lines up a shot and tracks incoming shells.</item>
    /// <item>Decisions: re-plans at a human cadence. It takes a firing position at its preferred range and
    /// circles the enemy, grabs pickups that are worth it, falls back to cover when it is losing the trade or
    /// reloading, and peeks out again when its gun is ready.</item>
    /// <item>Hands: drives with throttle inertia, backs up instead of three-point turning, aims with a
    /// wandering hand, leads moving targets, charges only as far as the range needs and dodges incoming fire.</item>
    /// </list>
    /// Difficulty changes how quickly and precisely it does all of this, not what it is allowed to do.
    /// </summary>
    public class TankAI : MonoBehaviour
    {
        private enum Goal { Engage, Cover, Collect, Unstuck }

        private struct Profile
        {
            public float Reaction;       // seconds between the enemy doing something and the bot seeing it
            public float AimWobble;      // degrees the aiming hand wanders
            public float AimTolerance;   // degrees off target it still accepts when releasing a shot
            public float Lead;           // 0..1 how well it leads a moving target
            public float ChargeError;    // spread of the charge it releases at (fraction of full charge)
            public float DodgeChance;    // chance it notices an incoming shot and dodges
            public float DodgeReaction;  // seconds before a dodge starts
            public float Think;          // seconds between re-planning
            public float Speed, Turn;    // drive and turn speed scale
            public float Hesitation;     // extra pause after the gun is ready again
            public float Aggression;     // 0..1 how close it likes to fight and how rarely it hides
            public float CoverHealth;    // health ratio under which it starts to play safe
        }

        private static readonly Profile[] Profiles =
        {
            new Profile { Reaction = .42f, AimWobble = 9f, AimTolerance = 8f, Lead = .3f, ChargeError = .12f,
                DodgeChance = .15f, DodgeReaction = .45f, Think = .75f, Speed = .85f, Turn = .85f, Hesitation = .9f,
                Aggression = .35f, CoverHealth = .2f },
            new Profile { Reaction = .28f, AimWobble = 5.5f, AimTolerance = 5.5f, Lead = .45f, ChargeError = .07f,
                DodgeChance = .3f, DodgeReaction = .3f, Think = .5f, Speed = 1f, Turn = 1f, Hesitation = .5f,
                Aggression = .55f, CoverHealth = .32f },
            new Profile { Reaction = .2f, AimWobble = 3.2f, AimTolerance = 4.5f, Lead = .65f, ChargeError = .05f,
                DodgeChance = .5f, DodgeReaction = .22f, Think = .38f, Speed = 1.05f, Turn = 1.1f, Hesitation = .35f,
                Aggression = .65f, CoverHealth = .38f },
        };

        private const float PowerUpSearchRadius = 18f;
        private static readonly float[] SpotAngles = { 0f, 25f, -25f, 55f, -55f, 90f, -90f };

        private Profile m_P = Profiles[1];
        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private PowerUpDetector m_PowerUpDetector;
        private TankHealth m_Health;
        private GameObject[] m_AllTanks = new GameObject[0];

        // perception
        private Transform m_Target;
        private Rigidbody m_TargetBody;
        private TankShooting m_TargetShooting;
        private TankHealth m_TargetHealth;
        private readonly List<Vector4> m_TargetTrail = new List<Vector4>();   // xyz position, w time
        private Vector3 m_SeenPos, m_SeenVel;
        private bool m_HasLos;
        private float m_LosTimer, m_ShellScanTimer;
        private readonly RaycastHit[] m_Hits = new RaycastHit[16];
        private readonly HashSet<ShellExplosion> m_JudgedShells = new HashSet<ShellExplosion>();
        private bool m_EnemyChargeJudged;

        // decisions
        private Goal m_Goal = Goal.Engage;
        private float m_ThinkTimer, m_GoalTimer, m_CoverHold, m_CoverCooldown, m_OrbitFlipTimer;
        private bool m_WantCover;
        private int m_OrbitSign = 1;
        private NavMeshPath m_Path;
        private int m_Corner;
        private PowerUp m_PowerUpTarget;

        // hands
        private float m_Throttle, m_WantThrottle;
        private Vector3 m_WantFacing;
        private float m_DodgeTimer, m_DodgeThrottle, m_PendingDodge = -1f;
        private Vector3 m_PendingDodgeFrom, m_PendingDodgeLine;
        private Vector3 m_DangerPoint;           // where an incoming shell will land
        private float m_DangerUntil = -1f;
        private float m_ShotLead = .5f;          // this shot's guess at how far to lead, rolled per shot
        private float m_UnstuckTimer, m_UnstuckThrottle, m_UnstuckTurn;
        private Vector3 m_StuckAnchor;
        private float m_StuckTimer;
        private bool m_Aiming;
        private Vector3 m_AimDir;
        private float m_AimCharge, m_ChargeNoise, m_ShotCooldown, m_Hesitate, m_OpeningDelay, m_AimSeed;
        private float[] m_RangeTable;
        private float m_MaxRange = 25f, m_HorizontalFactor = 1f;

        private void Awake()
        {
            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();
            m_PowerUpDetector = GetComponent<PowerUpDetector>();
            m_Health = GetComponent<TankHealth>();
            if (isActiveAndEnabled)
            {
                if (m_Movement != null) m_Movement.m_IsComputerControlled = true;
                if (m_Shooting != null) m_Shooting.m_IsComputerControlled = true;
            }
            m_AimSeed = Random.value * 100f;
            // Works without a GameManager too, so a bot can be dropped into any scene.
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude).Select(e => e.gameObject).ToArray();
        }

        public void Setup(GameManager manager)
        {
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
            ApplyDifficulty(TankDuelData.AIDifficulty);
        }

        public void ApplyDifficulty(int difficulty)
        {
            m_P = Profiles[Mathf.Clamp(difficulty, 0, Profiles.Length - 1)];
            m_ShotLead = m_P.Lead;
        }

        public void TurnOff() => enabled = false;

        private void OnEnable()
        {
            m_Goal = Goal.Engage;
            m_Path = null;
            m_PowerUpTarget = null;
            m_TargetTrail.Clear();
            m_JudgedShells.Clear();
            m_Throttle = m_WantThrottle = 0f;
            m_WantFacing = Vector3.zero;
            m_DodgeTimer = m_UnstuckTimer = m_ShotCooldown = m_Hesitate = m_GoalTimer = m_CoverCooldown = 0f;
            m_PendingDodge = -1f;
            m_Aiming = m_WantCover = m_EnemyChargeJudged = false;
            // A short look around before the first move, and nobody fires the instant a round starts.
            m_ThinkTimer = Random.Range(.1f, .3f) + m_P.Reaction;
            m_OpeningDelay = .6f + m_P.Reaction + Random.Range(0f, .4f);
            m_StuckAnchor = transform.position;
            m_StuckTimer = 0f;
            m_RangeTable = null;
        }

        private void OnDisable()
        {
            m_Path = null;
            m_PowerUpTarget = null;
            m_Aiming = false;
            m_Throttle = m_WantThrottle = 0f;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.deltaTime;
            if (m_RangeTable == null) BuildRangeTable();

            float cooldownScale = m_PowerUpDetector != null ? m_PowerUpDetector.ShootingCooldownMultiplier : 1f;
            if (m_ShotCooldown > 0f) m_ShotCooldown -= dt / cooldownScale;
            if (m_ShotCooldown <= 0f) m_Hesitate -= dt;
            m_OpeningDelay -= dt;
            m_CoverCooldown -= dt;
            m_OrbitFlipTimer -= dt;
            m_GoalTimer += dt;

            Perceive(dt);
            WatchThreats(dt);
            m_ThinkTimer -= dt;
            if (m_ThinkTimer <= 0f)
            {
                m_ThinkTimer = m_P.Think * Random.Range(.7f, 1.3f);
                Think();
            }
            Steer(dt);
            Shoot();

            if (Telemetry && (m_LogTimer -= dt) <= 0f)
            {
                m_LogTimer = 2f;
                Log($"goal={m_Goal} hp={Ratio(m_Health):0.00} pos=({transform.position.x:0.0},{transform.position.z:0.0}) " +
                    $"dist={(m_Target != null ? FlatDist(transform.position, m_Target.position) : -1f):0.0} los={m_HasLos} " +
                    $"throttle={m_Throttle:0.00}");
            }
        }

        /// <summary>Set by the "-botduel" soak test to write what the bots do to the player log.</summary>
        public static bool Telemetry;
        private float m_LogTimer;

        private void Log(string message) => Debug.Log($"[BotDuel] {name} t={Time.timeSinceLevelLoad:0.0} {message}");

        // ---------------------------------------------------------------- perception

        private void Perceive(float dt)
        {
            PickTarget();
            if (m_Target == null)
            {
                m_HasLos = false;
                return;
            }
            float now = Time.time;
            Vector3 p = m_Target.position;
            m_TargetTrail.Add(new Vector4(p.x, p.y, p.z, now));
            while (m_TargetTrail.Count > 2 && now - m_TargetTrail[1].w > m_P.Reaction + .8f)
                m_TargetTrail.RemoveAt(0);
            m_SeenPos = TrailAt(now - m_P.Reaction);
            m_SeenVel = Flat(m_SeenPos - TrailAt(now - m_P.Reaction - .45f)) / .45f;

            m_LosTimer -= dt;
            if (m_LosTimer <= 0f)
            {
                m_LosTimer = .1f;
                m_HasLos = ClearShot(transform.position, m_Target.position);
            }
        }

        private void PickTarget()
        {
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var tank in m_AllTanks)
            {
                if (tank == null || tank == gameObject || !tank.activeInHierarchy) continue;
                float d = FlatDist(transform.position, tank.transform.position);
                if (tank.transform == m_Target) d *= .8f;   // don't flip between targets every frame
                if (d < bestDist)
                {
                    bestDist = d;
                    best = tank.transform;
                }
            }
            if (best == m_Target) return;
            m_Target = best;
            m_TargetTrail.Clear();
            m_EnemyChargeJudged = false;
            m_TargetBody = best != null ? best.GetComponent<Rigidbody>() : null;
            m_TargetShooting = best != null ? best.GetComponent<TankShooting>() : null;
            m_TargetHealth = best != null ? best.GetComponent<TankHealth>() : null;
        }

        private Vector3 TrailAt(float time)
        {
            for (int i = m_TargetTrail.Count - 1; i >= 0; i--)
            {
                if (m_TargetTrail[i].w > time) continue;
                if (i == m_TargetTrail.Count - 1) return m_TargetTrail[i];
                Vector4 a = m_TargetTrail[i], b = m_TargetTrail[i + 1];
                float t = Mathf.InverseLerp(a.w, b.w, time);
                return Vector3.Lerp(a, b, t);
            }
            return m_TargetTrail[0];
        }

        /// <summary>True when nothing solid but tanks lies between the two hull positions at gun height.</summary>
        private bool ClearShot(Vector3 from, Vector3 to)
        {
            Vector3 a = from + Vector3.up * 1.1f, b = to + Vector3.up * 1.1f;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < .01f) return true;
            int n = Physics.RaycastNonAlloc(a, d / len, m_Hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var body = m_Hits[i].collider.attachedRigidbody;
                if (body != null && body.GetComponent<TankMovement>() != null) continue;
                return false;
            }
            return true;
        }

        private void WatchThreats(float dt)
        {
            if (m_DodgeTimer > 0f) m_DodgeTimer -= dt;
            if (m_PendingDodge >= 0f)
            {
                m_PendingDodge -= dt;
                if (m_PendingDodge < 0f) BeginDodge(m_PendingDodgeFrom, m_PendingDodgeLine);
            }

            // The enemy is holding fire with its gun on us: a sharp player jinks before the shot leaves.
            if (m_TargetShooting != null)
            {
                if (!m_TargetShooting.IsCharging) m_EnemyChargeJudged = false;
                else if (!m_EnemyChargeJudged)
                {
                    Vector3 toMe = Flat(transform.position - m_Target.position);
                    if (toMe.magnitude < m_MaxRange * 1.1f && Vector3.Angle(Flat(m_Target.forward), toMe) < 14f)
                    {
                        m_EnemyChargeJudged = true;
                        if (Random.value < m_P.DodgeChance * .7f)
                            ScheduleDodge(transform.position, Flat(m_Target.forward).normalized);
                    }
                }
            }

            // Shells already in the air.
            m_ShellScanTimer -= dt;
            if (m_ShellScanTimer > 0f) return;
            m_ShellScanTimer = .08f;
            var shells = ShellExplosion.Active;
            if (shells.Count == 0)
            {
                m_JudgedShells.Clear();
                return;
            }
            float g = Physics.gravity.y;
            for (int i = 0; i < shells.Count; i++)
            {
                var shell = shells[i];
                if (shell == null || shell.HasExploded || shell.Body == null) continue;
                if (m_JudgedShells.Contains(shell)) continue;
                Vector3 p = shell.Body.position, v = shell.Body.linearVelocity;
                if (Vector3.Dot(Flat(v), Flat(transform.position - p)) <= 0f)
                {
                    m_JudgedShells.Add(shell);   // flying away from us (our own shot)
                    continue;
                }
                float disc = v.y * v.y - 2f * g * p.y;
                if (disc < 0f || g >= 0f) continue;
                float t = (-v.y - Mathf.Sqrt(disc)) / g;
                Vector3 landing = p + Flat(v) * t;
                if (t > 1.5f || FlatDist(landing, transform.position) > shell.m_ExplosionRadius * .8f + 1f) continue;
                m_JudgedShells.Add(shell);
                // Even without dodging, don't drive into the blast.
                m_DangerPoint = landing;
                m_DangerUntil = Time.time + t + .3f;
                if (Random.value < m_P.DodgeChance) ScheduleDodge(landing, Flat(v).normalized);
            }
        }

        private void ScheduleDodge(Vector3 threat, Vector3 line)
        {
            if (m_DodgeTimer > 0f || m_PendingDodge >= 0f) return;
            m_PendingDodgeFrom = threat;
            m_PendingDodgeLine = line;
            m_PendingDodge = m_P.DodgeReaction * Random.Range(.8f, 1.25f);
        }

        private void BeginDodge(Vector3 threat, Vector3 line)
        {
            // A tank can only drive along its hull, so the dodge is a burst forwards or backwards.
            // Both ends are scored: the one further from the blast and off the shot's line wins,
            // as long as no wall is in the way. A dodge that would not get clear is not attempted.
            Vector3 pos = transform.position;
            Vector3 f = Flat(transform.forward).normalized;
            float reach = m_Movement != null ? m_Movement.m_Speed * m_P.Speed * .6f : 4f;
            float bestSign = 0f, bestScore = float.MinValue;
            foreach (float sign in new[] { 1f, -1f })
            {
                if (!DriveClear(f * sign, reach)) continue;
                Vector3 end = pos + f * (sign * reach);
                float score = FlatDist(end, threat);
                if (line.sqrMagnitude > .01f)
                {
                    Vector3 off = Flat(end - threat);
                    score += (off - line * Vector3.Dot(off, line)).magnitude * 1.5f;   // sideways off the line
                }
                score += Random.Range(0f, .3f);
                if (score <= bestScore) continue;
                bestScore = score;
                bestSign = sign;
            }
            if (bestSign == 0f || bestScore < (line.sqrMagnitude > .01f ? 2f : 3f)) return;
            m_DodgeThrottle = bestSign;
            m_DodgeTimer = Random.Range(.55f, .9f);
            if (Telemetry) Log($"dodge {(bestSign > 0 ? "forward" : "back")}");
        }

        private bool DriveClear(Vector3 dir, float distance)
        {
            if (!NavMesh.SamplePosition(transform.position, out var start, 2f, NavMesh.AllAreas)) return true;
            return !NavMesh.Raycast(start.position, start.position + dir * distance, out _, NavMesh.AllAreas);
        }

        // ---------------------------------------------------------------- decisions

        private void Think()
        {
            if (m_Goal == Goal.Unstuck) return;
            if (m_Target == null)
            {
                m_Goal = Goal.Engage;
                m_Path = null;
                return;
            }
            float hp = Ratio(m_Health), enemyHp = Ratio(m_TargetHealth);

            if (m_Goal == Goal.Collect && m_PowerUpTarget != null && m_GoalTimer < 6f &&
                m_PowerUpTarget.CanBeCollectedBy(m_PowerUpDetector))
                return;
            if (TryChoosePowerUp(hp)) return;

            if (m_Goal == Goal.Cover)
            {
                // Peek out again once the gun is loaded, or when hiding has gone on long enough.
                bool ready = m_ShotCooldown <= 0f && m_GoalTimer > .8f;
                if (m_GoalTimer < m_CoverHold && !ready) return;
                m_CoverCooldown = Random.Range(3f, 6f);
            }
            else
            {
                bool losing = hp < m_P.CoverHealth && hp <= enemyHp + .05f;
                if ((m_WantCover || (losing && m_ShotCooldown > .3f)) && m_CoverCooldown <= 0f && TryFindCover())
                {
                    m_WantCover = false;
                    return;
                }
            }
            m_WantCover = false;
            ChooseFiringSpot();
        }

        private void SetGoal(Goal goal, NavMeshPath path)
        {
            if (goal != m_Goal) m_GoalTimer = 0f;
            m_Goal = goal;
            m_Path = path;
            m_Corner = 1;
        }

        private void ChooseFiringSpot()
        {
            float preferred = PreferredRange();
            if (m_OrbitFlipTimer <= 0f)
            {
                m_OrbitFlipTimer = Random.Range(3f, 7f);
                if (Random.value < .5f) m_OrbitSign = -m_OrbitSign;
            }
            Vector3 enemy = m_SeenPos;
            Vector3 fromEnemy = Flat(transform.position - enemy);
            fromEnemy = fromEnemy.sqrMagnitude > .01f ? fromEnemy.normalized : -Flat(m_Target.forward).normalized;

            NavMeshPath best = null;
            bool bestSees = false;
            float bestScore = float.MaxValue;
            foreach (float angle in SpotAngles)
            {
                foreach (float rangeScale in new[] { 1f, .7f })
                {
                    float r = preferred * rangeScale;
                    Vector3 spot = enemy + Quaternion.AngleAxis(angle, Vector3.up) * fromEnemy * r;
                    if (!NavMesh.SamplePosition(spot, out var hit, 4f, NavMesh.AllAreas)) continue;
                    if (!PathTo(hit.position, out var path, out float length)) continue;
                    bool sees = ClearShot(hit.position, enemy);
                    float score = length + Mathf.Abs(FlatDist(hit.position, enemy) - preferred) * .3f;
                    if (sees) score -= 12f;
                    if (angle != 0f && Mathf.Sign(angle) == m_OrbitSign) score -= 3f;   // keep circling one way
                    if (length < 1.5f) score += 4f;                                     // don't sit still
                    if (Time.time < m_DangerUntil && FlatDist(hit.position, m_DangerPoint) < 5f) score += 15f;
                    score += Random.Range(0f, 2.5f);
                    if (score >= bestScore) continue;
                    bestScore = score;
                    best = path;
                    bestSees = sees;
                }
            }
            // No reachable spot sees the enemy: push towards it, the shot opens up on the way.
            if (!bestSees && NavMesh.SamplePosition(enemy, out var e, 3f, NavMesh.AllAreas) &&
                PathTo(e.position, out var direct, out _))
                best = direct;
            SetGoal(Goal.Engage, best);
        }

        private bool TryFindCover()
        {
            Vector3 enemy = m_SeenPos;
            NavMeshPath best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < 10; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(i * 36f + Random.Range(-10f, 10f), Vector3.up) * Vector3.forward;
                foreach (float r in new[] { 6f, 10f })
                {
                    if (!NavMesh.SamplePosition(transform.position + dir * r, out var hit, 2f, NavMesh.AllAreas)) continue;
                    if (FlatDist(hit.position, enemy) < 8f || ClearShot(hit.position, enemy)) continue;
                    if (!PathTo(hit.position, out var path, out float length) || length > 16f) continue;
                    float score = length - FlatDist(hit.position, enemy) * .3f + Random.Range(0f, 2f);
                    if (score >= bestScore) continue;
                    bestScore = score;
                    best = path;
                }
            }
            if (best == null) return false;
            SetGoal(Goal.Cover, best);
            m_GoalTimer = 0f;
            m_CoverHold = Random.Range(1.5f, 3f);
            return true;
        }

        private bool TryChoosePowerUp(float hp)
        {
            if (m_PowerUpDetector == null || !m_PowerUpDetector.CanCollectPowerUp) return false;
            float enemyDist = m_Target != null ? FlatDist(transform.position, m_SeenPos) : float.MaxValue;
            bool brawling = enemyDist < 10f && m_HasLos && hp > .5f;

            PowerUp best = null;
            NavMeshPath bestPath = null;
            float bestScore = float.MaxValue;
            var pickups = PowerUp.ActivePowerUps;
            for (int i = 0; i < pickups.Count; i++)
            {
                var pickup = pickups[i];
                if (pickup == null || !pickup.CanBeCollectedBy(m_PowerUpDetector)) continue;
                bool healing = pickup.Type == PowerUp.PowerUpType.Healing;
                if (healing && hp >= .98f) continue;
                Vector3 at = pickup.transform.position;
                if (FlatDist(at, transform.position) > PowerUpSearchRadius) continue;
                if (!NavMesh.SamplePosition(at, out var hit, 2f, NavMesh.AllAreas) || FlatDist(hit.position, at) > 1f) continue;
                if (!PathTo(hit.position, out var path, out float length) || length > PowerUpSearchRadius * 1.4f) continue;
                if (brawling && !healing && length > 6f) continue;
                // Don't race the enemy to a pickup it will clearly reach first.
                if (m_Target != null && FlatDist(m_SeenPos, at) < length * .6f && !healing) continue;
                float score = healing && hp < .7f ? length * .4f : length;
                if (score >= bestScore) continue;
                bestScore = score;
                best = pickup;
                bestPath = path;
            }
            if (best == null) return false;
            m_PowerUpTarget = best;
            SetGoal(Goal.Collect, bestPath);
            return true;
        }

        // Shells can fly further than the arenas are wide, so the comfortable fighting distance is capped.
        private float PreferredRange() =>
            Mathf.Min(m_MaxRange * Mathf.Lerp(.85f, .5f, m_P.Aggression), Mathf.Lerp(24f, 13f, m_P.Aggression));

        // ---------------------------------------------------------------- hands

        private void Steer(float dt)
        {
            Vector3 pos = transform.position;
            Vector3 f = Flat(transform.forward).normalized;

            CheckStuck(dt, pos);
            if (m_Goal == Goal.Unstuck)
            {
                m_Aiming = false;
                m_UnstuckTimer -= dt;
                m_WantThrottle = m_UnstuckThrottle;
                m_WantFacing = Quaternion.AngleAxis(60f * m_UnstuckTurn, Vector3.up) * f;
                if (m_UnstuckTimer <= 0f)
                {
                    m_Goal = Goal.Engage;
                    m_Path = null;
                    m_ThinkTimer = 0f;
                }
                return;
            }

            // where the route goes next
            Vector3 moveDir = Vector3.zero;
            float remaining = 0f;
            if (m_Path != null && m_Path.corners != null)
            {
                var corners = m_Path.corners;
                while (m_Corner < corners.Length && FlatDist(pos, corners[m_Corner]) < 1.2f) m_Corner++;
                if (m_Corner < corners.Length)
                {
                    Vector3 to = Flat(corners[m_Corner] - pos);
                    moveDir = to.normalized;
                    remaining = to.magnitude;
                    for (int i = m_Corner + 1; i < corners.Length; i++) remaining += FlatDist(corners[i - 1], corners[i]);
                }
            }

            float dist = m_Target != null ? FlatDist(pos, m_SeenPos) : float.MaxValue;
            bool gunReady = m_Shooting != null && (m_Shooting.IsCharging || (m_ShotCooldown <= 0f && m_Hesitate <= 0f));
            m_Aiming = m_Target != null && m_HasLos && m_OpeningDelay <= 0f && gunReady && dist <= m_MaxRange * 1.05f;

            if (m_Aiming)
            {
                Vector3 aimPoint = ComputeAimPoint(out m_AimCharge);
                float wobble = m_P.AimWobble * (Mathf.PerlinNoise(Time.time * .9f, m_AimSeed) * 2f - 1f);
                m_AimDir = Quaternion.AngleAxis(wobble, Vector3.up) * Flat(aimPoint - pos).normalized;
                m_WantFacing = m_AimDir;
                // Keep the range while lining up: creep in, back off, or rock to stay a moving target.
                float preferred = PreferredRange();
                float along = dist > preferred + 3f ? .55f
                    : dist < Mathf.Max(5f, preferred - 4f) ? -.6f
                    : Mathf.Sin(Time.time * 2.3f + m_AimSeed) * .35f;
                if (m_DodgeTimer > 0f) along = m_DodgeThrottle;
                m_WantThrottle = along * (Vector3.Dot(f, m_AimDir) > .5f ? 1f : .3f);
                return;
            }

            if (m_DodgeTimer > 0f)
            {
                m_WantFacing = f;
                m_WantThrottle = m_DodgeThrottle;
                return;
            }

            if (moveDir != Vector3.zero)
            {
                float dot = Vector3.Dot(f, moveDir);
                bool reverse = dot < -.35f && remaining < 9f;   // back up rather than turn around for a short hop
                if (m_Goal == Goal.Cover && m_Target != null && dot < 0f &&
                    Vector3.Dot(f, Flat(m_SeenPos - pos).normalized) > .3f)
                    reverse = true;                              // retreat with the strong front armour to the enemy
                if (reverse)
                {
                    m_WantFacing = -moveDir;
                    m_WantThrottle = -(dot < -.75f ? 1f : Mathf.Clamp01((-dot - .1f) / .65f));
                }
                else
                {
                    m_WantFacing = moveDir;
                    m_WantThrottle = dot > .75f ? 1f : Mathf.Clamp01((dot - .1f) / .65f);
                }
                if (remaining < 2.5f) m_WantThrottle *= Mathf.Lerp(.35f, 1f, remaining / 2.5f);
                // A shell is about to land on the route: wait for it instead of driving into it.
                if (Time.time < m_DangerUntil)
                {
                    Vector3 ahead = pos + m_WantFacing * (Mathf.Sign(m_WantThrottle) * 3f);
                    if (FlatDist(ahead, m_DangerPoint) < 4.5f && FlatDist(ahead, m_DangerPoint) < FlatDist(pos, m_DangerPoint))
                        m_WantThrottle = 0f;
                }
                return;
            }

            // Arrived: face the enemy, ready for the next shot, and plan the next move soon.
            m_WantFacing = m_Target != null ? Flat(m_SeenPos - pos).normalized : f;
            m_WantThrottle = 0f;
            m_ThinkTimer = Mathf.Min(m_ThinkTimer, m_HasLos ? .2f : .1f);
        }

        private void CheckStuck(float dt, Vector3 pos)
        {
            m_StuckTimer += dt;
            if (m_StuckTimer < 1f) return;
            if (m_Goal != Goal.Unstuck && Mathf.Abs(m_Throttle) > .5f && FlatDist(pos, m_StuckAnchor) < .5f)
            {
                m_Goal = Goal.Unstuck;
                m_UnstuckTimer = Random.Range(.5f, .8f);
                m_UnstuckThrottle = -Mathf.Sign(m_Throttle);
                m_UnstuckTurn = Random.value < .5f ? 1f : -1f;
            }
            m_StuckAnchor = pos;
            m_StuckTimer = 0f;
        }

        private void Shoot()
        {
            if (m_Shooting == null) return;
            Vector3 f = Flat(transform.forward).normalized;
            float error = Vector3.Angle(f, m_AimDir);

            if (!m_Shooting.IsCharging)
            {
                // Press the trigger while still swinging onto the target, like a player does.
                if (!m_Aiming || m_ShotCooldown > 0f || error > 30f) return;
                m_Shooting.StartCharging();
                if (m_Shooting.IsCharging)
                {
                    m_ChargeNoise = Gaussian() * m_P.ChargeError;
                    // People misjudge how far to lead, and now and then just shoot where the tank is.
                    m_ShotLead = Random.value < .2f ? 0f : m_P.Lead * Random.Range(.5f, 1.2f);
                }
                return;
            }

            // Charging: let go once the shell will reach and the gun is on target.
            float goal = Mathf.Clamp01(m_AimCharge + m_ChargeNoise);
            float tolerance = m_Aiming ? m_P.AimTolerance : m_P.AimTolerance * 2f;
            if (m_Shooting.CurrentChargeRatio + .01f < goal || error > tolerance) return;
            if (Telemetry)
                Log($"fire charge={m_Shooting.CurrentChargeRatio:0.00} want={goal:0.00} err={error:0.0} " +
                    $"dist={FlatDist(transform.position, m_SeenPos):0.0} los={m_HasLos}");
            m_Shooting.StopCharging();
            m_ShotCooldown = m_Shooting.m_ShotCooldown + .05f;
            m_Hesitate = m_P.Hesitation * Random.Range(.5f, 1.5f);
            m_ShotLead = Random.value < .2f ? 0f : m_P.Lead * Random.Range(.5f, 1.2f);
            // Shoot and scoot: cautious players duck back while reloading.
            if (Random.value < (1f - m_P.Aggression) * .6f)
            {
                m_WantCover = true;
                m_ThinkTimer = 0f;
            }
        }

        private Vector3 ComputeAimPoint(out float charge)
        {
            Vector3 pos = transform.position;
            // Skilled players also make up for their own reaction time.
            Vector3 target = m_SeenPos + m_SeenVel * (m_P.Reaction * m_ShotLead);
            Vector3 aim = target;
            charge = 0f;
            for (int i = 0; i < 2; i++)
            {
                float distance = FlatDist(pos, aim);
                charge = ChargeFor(distance);
                float stillToCharge = m_Shooting.IsCharging
                    ? Mathf.Max(0f, charge - m_Shooting.CurrentChargeRatio) * m_Shooting.m_MaxChargeTime
                    : charge * m_Shooting.m_MaxChargeTime + .1f;
                aim = target + m_SeenVel * (m_ShotLead * (FlightTime(charge, distance) + stillToCharge));
            }
            return aim;
        }

        private void BuildRangeTable()
        {
            m_RangeTable = new float[21];
            if (m_Shooting == null)
            {
                for (int i = 0; i < m_RangeTable.Length; i++) m_RangeTable[i] = 25f * i / 20f;
            }
            else
            {
                float last = 0f;
                for (int i = 0; i < m_RangeTable.Length; i++)
                {
                    last = Mathf.Max(last, FlatDist(transform.position, m_Shooting.GetProjectilePosition(i / 20f)));
                    m_RangeTable[i] = last;
                }
                Transform firePoint = m_Shooting.m_FireTransform != null ? m_Shooting.m_FireTransform : transform;
                m_HorizontalFactor = Mathf.Max(.2f, Flat(firePoint.forward).magnitude);
            }
            m_MaxRange = Mathf.Max(5f, m_RangeTable[m_RangeTable.Length - 1]);
            if (Telemetry)
                Log($"scene={gameObject.scene.name} range min={m_RangeTable[0]:0.0} max={m_MaxRange:0.0} " +
                    $"difficulty speed={m_P.Speed:0.00}");
        }

        private float ChargeFor(float distance)
        {
            var t = m_RangeTable;
            if (distance <= t[0]) return 0f;
            for (int i = 1; i < t.Length; i++)
            {
                if (t[i] < distance) continue;
                float span = Mathf.Max(.001f, t[i] - t[i - 1]);
                return (i - 1 + (distance - t[i - 1]) / span) / (t.Length - 1);
            }
            return 1f;
        }

        // Close targets are hit on the rising part of the arc, so the shell only flies as far as the target.
        private float FlightTime(float charge, float distance)
        {
            if (m_Shooting == null) return .5f;
            float idx = charge * (m_RangeTable.Length - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(idx), 0, m_RangeTable.Length - 2);
            float range = Mathf.Lerp(m_RangeTable[i], m_RangeTable[i + 1], idx - i);
            float speed = Mathf.Lerp(m_Shooting.m_MinLaunchForce, m_Shooting.m_MaxLaunchForce, charge) * m_HorizontalFactor;
            return Mathf.Min(range, distance) / Mathf.Max(.1f, speed);
        }

        private void FixedUpdate()
        {
            if (m_Movement == null) return;
            var rb = m_Movement.Rigidbody;
            if (rb == null) return;
            float dt = Time.fixedDeltaTime;

            // Throttle builds up and dies off like a player's thumb on the stick.
            m_Throttle = Mathf.MoveTowards(m_Throttle, m_WantThrottle, dt * 4.5f);
            Vector3 forward = Flat(rb.rotation * Vector3.forward).normalized;

            if (m_WantFacing.sqrMagnitude > .01f)
            {
                float angle = Vector3.SignedAngle(forward, m_WantFacing, Vector3.up);
                float step = m_Movement.m_TurnSpeed * m_P.Turn * dt;
                angle = Mathf.Clamp(angle, -step, step);
                if (Mathf.Abs(angle) > .0001f) rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(angle, Vector3.up));
            }
            if (Mathf.Abs(m_Throttle) > .001f)
                rb.MovePosition(rb.position + forward * (m_Throttle * m_Movement.m_Speed * m_P.Speed * dt));
        }

        // ---------------------------------------------------------------- helpers

        private bool PathTo(Vector3 destination, out NavMeshPath path, out float length)
        {
            path = new NavMeshPath();
            length = 0f;
            if (!NavMesh.SamplePosition(transform.position, out var start, 3f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position, destination, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
                return false;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return true;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float FlatDist(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

        private static float Ratio(TankHealth health) =>
            health != null ? health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) : 1f;

        private static float Gaussian()
        {
            float u = Mathf.Max(1e-5f, Random.value), v = Random.value;
            return Mathf.Clamp(Mathf.Sqrt(-2f * Mathf.Log(u)) * Mathf.Cos(2f * Mathf.PI * v), -2.5f, 2.5f);
        }
    }
}
