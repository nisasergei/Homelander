using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Главный компонент юнита: здоровье, защита, команда, награда, смерть/респаун.
// Висит на героях, крипах и зданиях (Barracks / Keeper).
public class Health : MonoBehaviour
{
    // Все живые и мёртвые (ожидающие респауна) юниты на карте — для быстрого поиска целей
    public static readonly List<Health> All = new List<Health>();

    [Header("Команда")]
    public int teamId = 1; // Номер команды/игрока (1, 2, 3, 4)
    public UnitKind unitKind = UnitKind.Creep;

    [Header("Здоровье")]
    public float maxHp = 100f;
    public float currentHp;
    [Tooltip("Регенерация HP в секунду")] public float hpRegen = 0f;

    [Header("Защита")]
    public float armor = 0f;
    public ArmorType armorType = ArmorType.Basic;
    [Tooltip("Снижение магического урона (0.25 = 25%)")]
    [Range(0f, 0.9f)] public float magicResistance = 0f;
    [Tooltip("Шанс увернуться от обычной атаки")]
    [Range(0f, 0.8f)] public float evasion = 0f;

    [Header("Передвижение")]
    [Tooltip("Скорость NavMeshAgent. 0 = не трогать значение агента")]
    public float moveSpeed = 0f;

    [Header("Награда за убийство")]
    public int goldBounty = 0;

    [Header("Смерть")]
    [Tooltip("Только для героев: время до воскрешения на месте старта")]
    public float respawnTime = 8f;

    [Header("Полоска HP")]
    public GameObject healthBarPrefab;
    public float healthBarHeight = 0f; // 0 = над коллайдером автоматически
    public float healthBarScale = 1f;

    public event Action<Health, DamageInfo, float> OnDamaged; // (кто, инфо, итоговый урон)
    public event Action<Health, Health> OnDied;              // (кто, убийца)
    public event Action<Health> OnRespawned;

    public bool IsAlive { get; private set; } = true;
    public float Radius { get; private set; } = 0.5f;
    public float HpPercent => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
    public float RespawnTimeLeft => IsAlive ? 0f : Mathf.Max(0f, respawnAt - Time.time);
    public float ArmorReductionPercent => CombatMath.ArmorReduction(armor) * 100f;

    NavMeshAgent agent;
    Vector3 spawnPosition;
    Quaternion spawnRotation;
    float respawnAt;

    void Awake()
    {
        currentHp = maxHp;
        agent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (agent != null && moveSpeed > 0f) agent.speed = moveSpeed;

        CalculateRadius();
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Start()
    {
        if (healthBarPrefab != null) CreateHealthBar();
    }

    void Update()
    {
        if (!IsAlive)
        {
            if (unitKind == UnitKind.Hero && Time.time >= respawnAt && !Keeper.IsEliminated(teamId))
                Respawn();
            return;
        }

        if (hpRegen > 0f && currentHp < maxHp)
            currentHp = Mathf.Min(maxHp, currentHp + hpRegen * Time.deltaTime);
    }

    void CalculateRadius()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 e = col.bounds.extents;
            Radius = Mathf.Max(0.2f, Mathf.Max(e.x, e.z));
        }
        else if (agent != null)
        {
            Radius = agent.radius;
        }
    }

    void CreateHealthBar()
    {
        GameObject bar = Instantiate(healthBarPrefab, transform);
        float height = healthBarHeight;
        if (height <= 0f)
        {
            Collider col = GetComponent<Collider>();
            height = col != null ? col.bounds.max.y - transform.position.y + 0.4f : 2f;
        }
        bar.transform.localPosition = new Vector3(0f, height / Mathf.Max(0.01f, transform.lossyScale.y), 0f);

        // Размер полоски задаёт сам HealthBar (в метрах), разметку префаба он переписывает
        HealthBar hb = bar.GetComponent<HealthBar>();
        if (hb != null) hb.Setup(this, healthBarScale);
    }

    // Старый вызов: физический урон без источника
    public void TakeDamage(float amount)
    {
        TakeDamage(new DamageInfo { amount = amount, damageType = DamageType.Physical, attackType = AttackType.Normal });
    }

    // Возвращает реально нанесённый урон (0 при промахе)
    public float TakeDamage(DamageInfo info)
    {
        if (!IsAlive || info.amount <= 0f) return 0f;

        if (info.isAttack && evasion > 0f && UnityEngine.Random.value < evasion)
        {
            DamagePopups.ShowMiss(this);
            return 0f;
        }

        float dmg = info.amount;
        switch (info.damageType)
        {
            case DamageType.Physical:
                dmg *= CombatMath.TypeMultiplier(info.attackType, armorType);
                dmg *= 1f - CombatMath.ArmorReduction(armor);
                break;
            case DamageType.Magical:
                dmg *= 1f - magicResistance;
                break;
        }

        dmg = Mathf.Max(1f, dmg);
        currentHp -= dmg;

        OnDamaged?.Invoke(this, info, dmg);
        DamagePopups.ShowDamage(this, dmg, info);

        if (currentHp <= 0f) Die(info.source);
        return dmg;
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    // Меняет максимум HP (например, рост силы волн). fill = восполнить до полного.
    public void SetMaxHp(float value, bool fill)
    {
        float pct = HpPercent;
        maxHp = Mathf.Max(1f, value);
        currentHp = fill ? maxHp : maxHp * pct;
    }

    public void Kill(Health killer = null)
    {
        if (!IsAlive) return;
        currentHp = 0f;
        Die(killer);
    }

    void Die(Health killer)
    {
        if (!IsAlive) return;
        IsAlive = false;
        currentHp = 0f;

        if (killer != null && killer.teamId != teamId && goldBounty > 0)
        {
            PlayerEconomy eco = PlayerEconomy.ForTeam(killer.teamId);
            if (eco != null) eco.AddGold(goldBounty);
            DamagePopups.ShowGold(this, goldBounty);
        }

        OnDied?.Invoke(this, killer);

        if (unitKind == UnitKind.Hero)
        {
            respawnAt = Time.time + respawnTime;
            SetHeroVisible(false);
            Debug.Log($"{name} погиб. Воскрешение через {respawnTime:0} с.");
        }
        else
        {
            if (unitKind == UnitKind.Building) Debug.Log($"{name} (игрок {teamId}) разрушен!");
            Destroy(gameObject);
        }
    }

    void Respawn()
    {
        IsAlive = true;
        currentHp = maxHp;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        SetHeroVisible(true);
        if (agent != null && agent.enabled) agent.Warp(spawnPosition);
        OnRespawned?.Invoke(this);
    }

    void SetHeroVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
        if (agent != null) agent.enabled = visible;
    }

    public bool IsEnemyOf(Health other)
    {
        return other != null && other.teamId != teamId;
    }
}
