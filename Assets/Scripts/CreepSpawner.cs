using UnityEngine;

public class CreepSpawner : MonoBehaviour
{
    public GameObject creepPrefab;
    public float spawnInterval = 2f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnCreep), 1f, spawnInterval);
    }

    void SpawnCreep()
    {
        if (creepPrefab != null)
        {
            Instantiate(creepPrefab, transform.position, Quaternion.identity);
        }
    }
}