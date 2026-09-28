using UnityEngine;
using UnityEngine.AI;

// Атака героя. Работает в двух режимах:
//  - герой игрока (есть PlayerClickMovement): бьёт то, что приказали ПКМ, а стоя на месте
//    сам атакует врагов в радиусе автоатаки;
//  - бот-защитник (нет PlayerClickMovement): охраняет свою базу в радиусе guardRadius
//    и возвращается на точку старта, когда врагов нет.
[RequireComponent(typeof(Health))]
public class HeroAttack : MonoBehaviour
{
    [Header("Атака")]
    public AttackStats attack = new AttackStats
    {
        damageMin = 42f,
        damageMax = 48f,
        attackRange = 1.5f,
        attackCooldown = 0.9f,
        attackPoint = 0.3f,
        attackType = AttackType.Hero,
        critChance = 0.15f,
        critMultiplier = 1.75f,
        lifesteal = 0.1f
    };

    [Header("Автоатака")]
    public bool autoAcquire = true;
    [Tooltip("Радиус, в котором стоящий герой сам начинает бить врагов")]
    public float acquireRange = 5f;

    [Header("Бот-защитник")]
    [Tooltip("Радиус охраны вокруг точки старта")]
    public float guardRadius = 11f;

    public float turnSpeed = 720f;
    public float repathInterval = 0.2f;

    public Health CurrentTarget => target;
    public bool IsBot { get; private set; }

    Health health;
    NavMeshAgent agent;
    AttackRunner runner;

    Health target;
    bool forcedTarget;          // цель назначена приказом игрока — преследуем без ограничений
    bool autoSuspended;         // игрок отдал приказ движения — не отвлекаемся до прибытия
    Vector3 home;
    float nextScanTime;
    float nextRepathTime;

    void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        health.unitKind = UnitKind.Hero;
        runner = new AttackRunner(health, attack);
        IsBot = GetComponent<PlayerClickMovement>() == null;
    }

    void Start()
    {
        home = transform.position;
        health.OnRespawned += _ => ClearTarget();
    }

    // ---------- Приказы от игрока ----------

    public void OrderAttack(Health newTarget)
    {
        if (newTarget == null || !health.IsEnemyOf(newTarget)) return;
        if (target != newTarget) runner.Cancel();
        target = newTarget;
        forcedTarget = true;
        autoSuspended = false;
        nextRepathTime = 0f;
    }

    public void OrderMove()
    {
        ClearTarget();
        autoSuspended = true;
    }

    public void ClearTarget()
    {
        target = null;
        forcedTarget = false;
        runner.Cancel();
    }

    // ---------- Логика ----------

    void Update()
    {
        if (!health.IsAlive) return;

        runner.UpdateWindup();

        if (target != null && !target.IsAlive) ClearTarget();

        if (autoSuspended && ReachedDestination()) autoSuspended = false;

        if (!forcedTarget && Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.25f;
            UpdateAutoTarget();
        }

        if (target != null)
        {
            if (runner.InRange(target) || runner.IsWindingUp)
            {
                Stop();
                CombatMath.FaceTowards(transform, target.transform.position, turnSpeed);
                runner.Tick(target);
            }
            else
            {
                Chase(target.transform.position);
            }
        }
        else if (IsBot)
        {
            ReturnHome();
        }
    }

    void UpdateAutoTarget()
    {
        if (!autoAcquire || autoSuspended) return;

        Vector3 center = IsBot ? home : transform.position;
        float radius = IsBot ? guardRadius : acquireRange;

        // Текущую авто-цель бросаем, если она ушла из зоны
        if (target != null)
        {
            if (FlatDistance(center, target.transform.position) <= radius + 2f) return;
            ClearTarget();
        }

        Health best = null;
        float bestDist = float.MaxValue;
        foreach (Health h in Health.All)
        {
            if (h == null || !h.IsAlive || !health.IsEnemyOf(h)) continue;
            if (FlatDistance(center, h.transform.position) > radius) continue;

            // Здания сами не атакуем — только по приказу
            if (h.unitKind == UnitKind.Building) continue;

            float d = CombatMath.EdgeDistance(health, h);
            if (d < bestDist)
            {
                bestDist = d;
                best = h;
            }
        }

        if (best != null)
        {
            target = best;
            nextRepathTime = 0f;
        }
    }

    void Chase(Vector3 pos)
    {
        if (!AgentReady()) return;
        agent.isStopped = false;
        if (Time.time >= nextRepathTime)
        {
            agent.SetDestination(pos);
            nextRepathTime = Time.time + repathInterval;
        }
    }

    void Stop()
    {
        if (!AgentReady()) return;
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
    }

    void ReturnHome()
    {
        if (!AgentReady()) return;
        if (FlatDistance(transform.position, home) > 1f)
        {
            agent.isStopped = false;
            if (!agent.hasPath && !agent.pathPending) agent.SetDestination(home);
        }
    }

    bool ReachedDestination()
    {
        if (!AgentReady()) return true;
        if (agent.pathPending) return false;
        return !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    bool AgentReady() => agent != null && agent.enabled && agent.isOnNavMesh;

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
