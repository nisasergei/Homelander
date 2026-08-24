using UnityEngine;
using UnityEngine.AI;

public class CreepPath : MonoBehaviour
{
    public int currentWaypointIndex = 2;
    public float attackDamage = 15f;
    public float attackRange = 3f;
    public float attackCooldown = 1.2f;

    private NavMeshAgent agent;
    private Health myHealth;
    private Transform currentTarget;
    private float lastAttackTime;
    private bool attackingBuilding = false;
    private Animator anim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myHealth = GetComponent<Health>();
        
        // Находим Animator, где бы он ни лежал (в родителе или дочерней модели)
        anim = GetComponentInChildren<Animator>();

        MoveToNextWaypoint();
    }

    void MoveToNextWaypoint()
    {
        attackingBuilding = false;

        GameObject targetObj = GameObject.Find($"Barracks_P{currentWaypointIndex}");
        if (targetObj == null)
        {
            targetObj = GameObject.Find($"Keeper_P{currentWaypointIndex}");
        }

        if (targetObj != null)
        {
            currentTarget = targetObj.transform;
            attackingBuilding = true;
            SetDestination(currentTarget.position);
        }
        else
        {
            GameObject wpObj = GameObject.Find($"WP{currentWaypointIndex}");
            if (wpObj != null)
            {
                currentTarget = wpObj.transform;
                SetDestination(currentTarget.position);
            }
            else
            {
                SwitchToNextCorner();
            }
        }
    }

    void SwitchToNextCorner()
    {
        currentWaypointIndex = (currentWaypointIndex % 4) + 1;
        MoveToNextWaypoint();
    }

    void SetDestination(Vector3 pos)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(pos);
        }
    }

    void Update()
    {
        // Включаем бег, если реальная скорость больше 0.1
        if (anim != null && agent != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            anim.SetBool("IsRunning", isMoving);
        }

        if (currentTarget == null)
        {
            SwitchToNextCorner();
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (!attackingBuilding && dist <= 2f)
        {
            SwitchToNextCorner();
            return;
        }

        if (attackingBuilding && dist <= attackRange)
        {
            if (agent.isOnNavMesh) agent.ResetPath();

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Health targetHp = currentTarget.GetComponent<Health>();
                if (targetHp != null)
                {
                    targetHp.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;
                }
            }
        }
        else if (agent.isOnNavMesh && currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
    }
}