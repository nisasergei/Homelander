using UnityEngine;

public class CreepHealth : MonoBehaviour
{
    public float hp = 100f;

    public void TakeDamage(float amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}