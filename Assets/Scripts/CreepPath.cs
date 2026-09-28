using UnityEngine;
using UnityEngine.AI;

// ИИ крипа: идёт по кругу карты (углы 1 → 2 → 3 → 4), по пути дерётся с вражескими
// крипами и героями, а на чужих базах ломает Barracks и Keeper. Свои постройки не трогает.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class CreepPath : MonoBehaviour
{
    [Header("Маршрут")]
    [Tooltip("К какому углу идти первым. Спавнер выставляет автоматически (следующий после своего)")]
    public int currentWaypointIndex = 2;
    public float waypointReachDistance = 2f;

    [Header("Атака")]
    public AttackStats attack = new AttackStats();

    [Header("Агро")]
    [Tooltip("Радиус, в котором крип замечает вражеских юнитов")]
    public float aggroRange = 5f;
    [Tooltip("Если цель убежала дальше этого — бросить погоню")]
    public float chaseLimit = 8f;
    public float targetScanInterval = 0.3f;

    [Header("Движение")]
    [Tooltip("Как часто пересчитывать путь до движущейся цели")]
    public float repathInterval = 0.35f;
    public float turnSpeed = 540f;

    [Header("Анимация (имена состояний в CreepAnimator)")]
    public string idleState = "Idle";
    public string runState = "Slow Run";
    public string attackState = "Standing Torch Melee Attack 03";
    [Tooltip("При этой скорости анимация бега играет с x1")]
    public float runAnimReferenceSpeed = 2.4f;

    NavMeshAgent agent;
    Health health;
    AttackRunner runner;
    Animator[] animators;

    Health target;            // кого бьём сейчас
    Transform waypoint;       // куда идём, если бить некого
    float nextScanTime;
    float nextRepathTime;
    Vector3 lastDestination = Vector3.positiveInfinity;
    int animState = -1;       // 0 idle, 1 run, 2 attack
    float attackAnimUntil;

    Transform model;          // визуальная модель (дочерний объект с Animator)
    Vector3 modelLocalPos;
    Quaternion modelLocalRot;

    // В префабе два Animator'а на одном скелете: на корне Creep и на вложенной модели.
    // Они дерутся друг с другом, а root motion анимации бега уводит модель от реального
    // крипа (NavMeshAgent/коллайдер/полоска HP остаются на месте, а человечек убегает).
    // Оставляем один Animator — на модели, выключаем root motion и держим модель на месте.
    void SetupModel()
    {
        Animator[] all = GetComponentsInChildren<Animator>(true);
        Animator modelAnimator = null;
        foreach (Animator a in all)
        {
            if (a.gameObject != gameObject) { modelAnimator = a; break; }
        }

        if (modelAnimator != null)
        {
            // Корневой Animator лишний — его контроллер уже продублирован на модели
            foreach (Animator a in all)
            {
                if (a == modelAnimator) continue;
                if (a.gameObject == gameObject && modelAnimator.runtimeAnimatorController == null)
                    modelAnimator.runtimeAnimatorController = a.runtimeAnimatorController;
                a.enabled = false;
            }
            animators = new[] { modelAnimator };
            model = modelAnimator.transform;
            modelLocalPos = model.localPosition;
            modelLocalRot = model.localRotation;
        }
        else
        {
            animators = all;
        }

        foreach (Animator a in animators) a.applyRootMotion = false;
    }

    // После анимации возвращаем модель на её место внутри крипа
    void LateUpdate()
    {
        if (model == null) return;
        model.localPosition = modelLocalPos;
        model.localRotation = modelLocalRot;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        health.unitKind = UnitKind.Creep;

        runner = new AttackRunner(health, attack);
        runner.OnAttackStarted += _ => PlayAttackAnim();

        SetupModel();

        // Плавнее и "проще": быстрый разворот, мягкий старт, случайный приоритет расталкивания
        agent.angularSpeed = turnSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, 10f);
        agent.autoBraking = true;
        agent.avoidancePriority = Random.Range(35, 65);
    }

    void Start()
    {
        // Небольшой разброс, чтобы вся волна не сканировала в один кадр
        nextScanTime = Time.time + Random.Range(0f, targetScanInterval);
        PickObjective();
    }

    void Update()
    {
        if (!health.IsAlive) return;

        runner.UpdateWindup();

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + targetScanInterval;
            ScanForTargets();
        }

        if (target != null && target.IsAlive)
            HandleCombat();
        else
            HandleWalking();

        UpdateAnimation();
    }

    // ---------------- Выбор цели ----------------

    void ScanForTargets()
    {
        // Держим текущую цель-юнита, пока она рядом
        if (target != null && target.IsAlive && target.unitKind != UnitKind.Building)
        {
            if (CombatMath.EdgeDistance(health, target) <= chaseLimit) return;
            target = null;
        }

        Health best = null;
        float bestScore = float.MaxValue;
        foreach (Health h in Health.All)
        {
            if (h == null || !h.IsAlive || !health.IsEnemyOf(h) || h.unitKind == UnitKind.Building) continue;
            float d = CombatMath.EdgeDistance(health, h);
            if (d > aggroRange) continue;

            // Крипы в приоритете перед героями (как в Dota)
            float score = d + (h.unitKind == UnitKind.Hero ? 1.5f : 0f);
            if (score < bestScore)
            {
                bestScore = score;
                best = h;
            }
        }

        if (best != null)
        {
            SetTarget(best);
            return;
        }

        // Юнитов рядом нет — идём ломать здания текущего угла
        if (target == null || !target.IsAlive) PickObjective();
    }

    void SetTarget(Health newTarget)
    {
        if (newTarget == target) return;
        target = newTarget;
        runner.Cancel();
        nextRepathTime = 0f;
    }

    void PickObjective()
    {
        target = null;
        waypoint = null;

        // Максимум один полный круг поиска, чтобы не зациклиться
        for (int i = 0; i < 4; i++)
        {
            Health building = FindEnemyBuilding(currentWaypointIndex);
            if (building != null)
            {
                SetTarget(building);
                return;
            }

            GameObject wp = GameObject.Find($"WP{currentWaypointIndex}");
            if (wp != null)
            {
                waypoint = wp.transform;
                return;
            }

            AdvanceCorner();
        }
    }

    Health FindEnemyBuilding(int corner)
    {
        if (corner == health.teamId) return null; // свои постройки не атакуем

        foreach (string prefix in new[] { "Barracks_P", "Keeper_P" })
        {
            GameObject obj = GameObject.Find(prefix + corner);
            if (obj == null) continue;
            Health h = obj.GetComponent<Health>();
            if (h != null && h.IsAlive && health.IsEnemyOf(h)) return h;
        }
        return null;
    }

    void AdvanceCorner()
    {
        currentWaypointIndex = (currentWaypointIndex % 4) + 1;
    }

    // ---------------- Поведение ----------------

    void HandleCombat()
    {
        if (runner.InRange(target) || runner.IsWindingUp)
        {
            StopMoving();
            CombatMath.FaceTowards(transform, target.transform.position, turnSpeed);
            runner.Tick(target);
        }
        else
        {
            MoveTo(target.transform.position);
        }
    }

    void HandleWalking()
    {
        if (target != null && !target.IsAlive) target = null;

        if (waypoint == null)
        {
            PickObjective();
            if (waypoint == null) { StopMoving(); return; }
        }

        Vector3 flat = waypoint.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude <= waypointReachDistance)
        {
            AdvanceCorner();
            PickObjective();
            return;
        }

        MoveTo(waypoint.position);
    }

    void MoveTo(Vector3 pos)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;

        // Не дёргаем агент каждый кадр — это даёт рывки и дрожание в толпе
        if (Time.time < nextRepathTime) return;

        bool moved = (pos - lastDestination).sqrMagnitude > 0.25f;
        bool noPath = !agent.hasPath && !agent.pathPending;
        if (moved || noPath)
        {
            agent.SetDestination(pos);
            lastDestination = pos;
        }
        nextRepathTime = Time.time + repathInterval;
    }

    void StopMoving()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
            lastDestination = Vector3.positiveInfinity;
        }
    }

    // ---------------- Анимация ----------------

    void PlayAttackAnim()
    {
        attackAnimUntil = Time.time + Mathf.Max(attack.attackPoint + 0.25f, attack.attackCooldown * 0.8f);
        animState = 2;
        foreach (Animator a in animators)
        {
            if (a == null || a.runtimeAnimatorController == null) continue;
            a.speed = 1f;
            if (a.HasState(0, Animator.StringToHash(attackState)))
                a.CrossFadeInFixedTime(attackState, 0.08f, 0, 0f);
        }
    }

    void UpdateAnimation()
    {
        if (Time.time < attackAnimUntil) return;

        float speed = agent.enabled ? agent.velocity.magnitude : 0f;
        bool running = speed > 0.15f;
        int wanted = running ? 1 : 0;

        foreach (Animator a in animators)
        {
            if (a == null || a.runtimeAnimatorController == null) continue;
            a.SetBool("IsRunning", running);
            a.speed = running ? Mathf.Clamp(speed / Mathf.Max(0.1f, runAnimReferenceSpeed), 0.6f, 1.3f) : 1f;

            if (wanted != animState)
            {
                string state = running ? runState : idleState;
                if (a.HasState(0, Animator.StringToHash(state)))
                    a.CrossFadeInFixedTime(state, 0.2f, 0);
            }
        }
        animState = wanted;
    }
}
