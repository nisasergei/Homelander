using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHp = 100f;
    public float currentHp;
    public int teamId = 1; // Номер команды/игрока (1, 2, 3, 4)

    void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        Debug.Log($"{gameObject.name} (Команда {teamId}) получил {amount} урона. Осталось HP: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} уничтожен!");
        Destroy(gameObject);
    }
}