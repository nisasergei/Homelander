using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Спавн крипов волнами. Висит на Barracks_Px.
// Команда берётся из Health казармы, первый угол маршрута — следующий по кругу.
// Можно повесить на любую казарму — всё определится само.
public class CreepSpawner : MonoBehaviour
{
    public GameObject creepPrefab;

    [Header("Волны")]
    public float firstWaveDelay = 5f;
    [Tooltip("Секунд между волнами")]
    public float waveInterval = 30f;
    public int creepsPerWave = 3;
    [Tooltip("Пауза между крипами внутри волны — идут цепочкой, а не кучей")]
    public float spawnSpacing = 1.5f;
    [Tooltip("На каком расстоянии от казармы появляются крипы (в сторону маршрута)")]
    public float spawnOffset = 2.5f;

    [Header("Рост силы (за каждую волну)")]
    [Range(0f, 0.2f)] public float hpGrowthPerWave = 0.04f;
    [Range(0f, 0.2f)] public float damageGrowthPerWave = 0.03f;
    [Tooltip("Каждые N волн в волне на одного крипа больше. 0 = выкл")]
    public int extraCreepEveryNWaves = 5;
    public int maxCreepsPerWave = 6;

    public int WaveNumber { get; private set; }
    public float NextWaveIn => Mathf.Max(0f, nextWaveAt - Time.time);

    Health myHealth;
    float nextWaveAt;

    void Start()
    {
        myHealth = GetComponent<Health>();
        nextWaveAt = Time.time + firstWaveDelay;
    }

    void Update()
    {
        if (Time.time < nextWaveAt) return;
        nextWaveAt = Time.time + waveInterval;
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        WaveNumber++;
        int count = creepsPerWave;
        if (extraCreepEveryNWaves > 0) count += (WaveNumber - 1) / extraCreepEveryNWaves;
        count = Mathf.Min(count, maxCreepsPerWave);

        for (int i = 0; i < count; i++)
        {
            SpawnCreep();
            if (i < count - 1) yield return new WaitForSeconds(spawnSpacing);
        }
    }

    public int TeamId => myHealth != null ? myHealth.teamId : 1;

    // Используется и волнами, и магазином
    public GameObject SpawnCreep()
    {
        if (creepPrefab == null) return null;

        int team = TeamId;
        int firstCorner = (team % 4) + 1;

        Vector3 pos = transform.position;
        GameObject wp = GameObject.Find($"WP{firstCorner}");
        if (wp != null)
        {
            Vector3 dir = wp.transform.position - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) pos += dir.normalized * spawnOffset;
        }
        pos += new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));

        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas)) pos = hit.position;

        GameObject creep = Instantiate(creepPrefab, pos, Quaternion.identity);

        Health h = creep.GetComponent<Health>();
        if (h != null)
        {
            h.teamId = team;
            float hpMult = 1f + hpGrowthPerWave * Mathf.Max(0, WaveNumber - 1);
            h.SetMaxHp(h.maxHp * hpMult, true);
        }

        CreepPath path = creep.GetComponent<CreepPath>();
        if (path != null)
        {
            path.currentWaypointIndex = firstCorner;
            path.attack.damageMultiplier = 1f + damageGrowthPerWave * Mathf.Max(0, WaveNumber - 1);
        }

        creep.name = $"Creep_P{team}";
        return creep;
    }
}
