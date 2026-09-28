using System;
using UnityEngine;

// ============================================================================
//  Общие типы и формулы боевки. ПОКА ЧТО ТАК
//  Урон проходит так:
//    1) атакующий бросает урон (min..max) и шанс крита
//    2) цель проверяет уклонение (только для обычных атак)
//    3) физический урон: множитель "тип атаки x тип брони", затем снижение от брони
//       магический урон: снижение от магического сопротивления
//       чистый урон: без снижений
// ============================================================================

public enum UnitKind { Hero, Creep, Building }

public enum DamageType { Physical, Magical, Pure }

public enum AttackType { Normal, Hero, Siege }

public enum ArmorType { Basic, Hero, Fortified }

public struct DamageInfo
{
    public float amount;
    public DamageType damageType;
    public AttackType attackType;
    public bool isAttack;   
    public bool isCrit;
    public Health source;
}

public static class CombatMath
{
    // Снижение урона от брони (пример по доте): 6% за единицу, с убывающей отдачей.
    // 5 брони ~ 23%, 10 брони ~ 37.5%, 20 брони ~ 54.5%. Отрицательная броня увеличивает урон.
    public static float ArmorReduction(float armor)
    {
        return 0.06f * armor / (1f + 0.06f * Mathf.Abs(armor));
    }
    
    static readonly float[,] matrix =
    {
        { 1.00f, 0.75f, 0.70f },
        { 1.00f, 1.00f, 0.50f },
        { 1.00f, 0.50f, 1.50f },
    };

    public static float TypeMultiplier(AttackType attack, ArmorType armor)
    {
        return matrix[(int)attack, (int)armor];
    }
    
    public static float EdgeDistance(Health a, Health b)
    {
        Vector3 d = a.transform.position - b.transform.position;
        d.y = 0f;
        return Mathf.Max(0f, d.magnitude - a.Radius - b.Radius);
    }

    public static void FaceTowards(Transform self, Vector3 point, float degreesPerSecond)
    {
        Vector3 dir = point - self.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        self.rotation = Quaternion.RotateTowards(self.rotation, look, degreesPerSecond * Time.deltaTime);
    }
}

[Serializable]
public class AttackStats
{
    [Tooltip("Минимальный урон за удар")] public float damageMin = 20f;
    [Tooltip("Максимальный урон за удар")] public float damageMax = 24f;
    [Tooltip("Дальность атаки (от края до края)")] public float attackRange = 1.2f;
    [Tooltip("Секунд между ударами")] public float attackCooldown = 1f;
    [Tooltip("Замах: через сколько секунд после начала атаки проходит урон")] public float attackPoint = 0.35f;
    public AttackType attackType = AttackType.Normal;
    public DamageType damageType = DamageType.Physical;
    [Range(0f, 1f)] public float critChance = 0f;
    public float critMultiplier = 1.75f;
    [Tooltip("Доля нанесённого урона, возвращаемая в HP")]
    [Range(0f, 1f)] public float lifesteal = 0f;

    [NonSerialized] public float damageMultiplier = 1f; // рост силы волн и т.п.

    public float AverageDamage => (damageMin + damageMax) * 0.5f * damageMultiplier;
    public float AttacksPerSecond => attackCooldown > 0f ? 1f / attackCooldown : 0f;

    public DamageInfo Roll(Health source)
    {
        float dmg = UnityEngine.Random.Range(damageMin, damageMax) * damageMultiplier;
        bool crit = critChance > 0f && UnityEngine.Random.value < critChance;
        if (crit) dmg *= critMultiplier;

        return new DamageInfo
        {
            amount = dmg,
            damageType = damageType,
            attackType = attackType,
            isAttack = true,
            isCrit = crit,
            source = source
        };
    }
}

// Рантайм-логика атаки с замахом и перезарядкой. Используется героями и крипами.
public class AttackRunner
{
    readonly Health owner;
    readonly AttackStats stats;

    float nextAttackTime;
    float hitTime;
    Health windupTarget;

    public event Action<Health> OnAttackStarted;

    public bool IsWindingUp => windupTarget != null;

    public AttackRunner(Health owner, AttackStats stats)
    {
        this.owner = owner;
        this.stats = stats;
    }

    public bool InRange(Health target)
    {
        return target != null && CombatMath.EdgeDistance(owner, target) <= stats.attackRange;
    }
    
    public void Tick(Health target)
    {
        UpdateWindup();
        if (IsWindingUp || target == null || !target.IsAlive) return;

        if (Time.time >= nextAttackTime)
        {
            windupTarget = target;
            hitTime = Time.time + stats.attackPoint;
            nextAttackTime = Time.time + stats.attackCooldown;
            OnAttackStarted?.Invoke(target);
        }
    }
    
    public void UpdateWindup()
    {
        if (windupTarget == null || Time.time < hitTime) return;

        Health target = windupTarget;
        windupTarget = null;
        
        if (target == null || !target.IsAlive) return;
        if (CombatMath.EdgeDistance(owner, target) > stats.attackRange + 1f) return;

        float dealt = target.TakeDamage(stats.Roll(owner));
        if (stats.lifesteal > 0f && dealt > 0f) owner.Heal(dealt * stats.lifesteal);
    }

    public void Cancel()
    {
        windupTarget = null;
    }
}
