using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Управление героем игрока: ПКМ по земле — идти, ПКМ по врагу — атаковать.
// Урон, дальность и скорость атаки задаются в HeroAttack, HP и броня — в Health.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HeroAttack))]
public class PlayerClickMovement : MonoBehaviour
{
    public static int LocalTeamId { get; private set; } = 1;
    public static PlayerClickMovement Local { get; private set; }

    public Camera mainCamera;

    NavMeshAgent agent;
    HeroAttack heroAttack;
    Health health;

    public Health Health => health;
    public HeroAttack Attack => heroAttack;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        heroAttack = GetComponent<HeroAttack>();
        health = GetComponent<Health>();
        if (health != null) LocalTeamId = health.teamId;
        Local = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null) return;
        if (health != null && !health.IsAlive) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // 1) Кликнули по врагу — атакуем (ищем Health и у родителя, у крипов модель вложена)
        foreach (RaycastHit hit in hits)
        {
            Health target = hit.collider.GetComponentInParent<Health>();
            if (target != null && target != health && target.IsAlive && target.teamId != LocalTeamId)
            {
                heroAttack.OrderAttack(target);
                return;
            }
        }

        // 2) Иначе — идём в точку на земле (первая поверхность, у которой нет Health)
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Health>() != null) continue;
            if (hit.collider.isTrigger) continue;

            if (agent.enabled && agent.isOnNavMesh)
            {
                heroAttack.OrderMove();
                agent.isStopped = false;
                agent.SetDestination(hit.point);
            }
            return;
        }
    }
}
