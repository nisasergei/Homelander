using UnityEngine;

public class Keeper : MonoBehaviour
{
    public int health = 1000;
    public int ownerPlayerId = 1;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Трон игрока {ownerPlayerId} атакован! ХП: {health}");

        if (health <= 0)
        {
            Debug.Log($"Игрок {ownerPlayerId} ВЫБЫЛ ИЗ ИГРЫ!");
            gameObject.SetActive(false); // Трон разрушен
        }
    }
}