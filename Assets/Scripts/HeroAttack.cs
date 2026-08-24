using UnityEngine;

public class HeroAttack : MonoBehaviour
{
    public float attackRange = 3f;
    public float attackDamage = 25f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    void Update()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Ищем всех крипов вокруг
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Creep"))
            {
                // Наносим урон крипу
                CreepHealth creep = hit.GetComponent<CreepHealth>();
                if (creep != null)
                {
                    creep.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;
                    break;
                }
            }
        }
    }
}