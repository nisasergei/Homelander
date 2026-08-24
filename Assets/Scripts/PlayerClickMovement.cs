using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerClickMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    public Camera mainCamera;
    
    public float attackRange = 2.5f;
    public float attackDamage = 30f;
    public float attackCooldown = 0.8f;
    
    private Transform attackTarget;
    private float lastAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Кликом ПКМ задаем движение или цель атаки
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Health targetHealth = hit.collider.GetComponent<Health>();
                // Если кликнули по врагу другого teamId
                if (targetHealth != null && targetHealth.teamId != 1) 
                {
                    attackTarget = targetHealth.transform;
                }
                else
                {
                    attackTarget = null;
                    agent.SetDestination(hit.point);
                }
            }
        }

        // Логика преследования и атаки цели
        if (attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, attackTarget.position);
            if (distance <= attackRange)
            {
                agent.ResetPath(); // Останавливаемся для атаки
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    Health targetHp = attackTarget.GetComponent<Health>();
                    if (targetHp != null)
                    {
                        targetHp.TakeDamage(attackDamage);
                        lastAttackTime = Time.time;
                    }
                }
            }
            else
            {
                agent.SetDestination(attackTarget.position); // Подносим героя ближе
            }
        }
    }
}