using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public PlayerEconomy playerEconomy;
    public Transform myBarracks;      // Ссылка на Barracks_P1
    public GameObject creepPrefab;    // Префаб крипа (если на казарме нет CreepSpawner)
    public TextMeshProUGUI goldText;  // Текст золота в UI

    [Header("Покупка крипа")]
    public int meleeCost = 50;
    public int meleeIncomeBonus = 10;

    [Header("Отладочная панель")]
    public bool showStatsPanel = true;

    void Update()
    {
        if (playerEconomy != null && goldText != null)
        {
            goldText.text = $"Золото: {playerEconomy.gold} | Инком: +{playerEconomy.income}";
        }
    }

    // Вызывается при клике по кнопке покупки
    public void BuyMeleeUnit()
    {
        if (playerEconomy == null || myBarracks == null) return; // казарма разрушена — покупать некуда
        if (Keeper.IsEliminated(playerEconomy.TeamId)) return;
        if (!playerEconomy.SpendGold(meleeCost)) return;

        playerEconomy.income += meleeIncomeBonus;

        CreepSpawner spawner = myBarracks.GetComponent<CreepSpawner>();
        if (spawner != null)
        {
            spawner.SpawnCreep();
        }
        else if (creepPrefab != null)
        {
            GameObject creep = Instantiate(creepPrefab, myBarracks.position + Vector3.forward * 2f, Quaternion.identity);
            Health h = creep.GetComponent<Health>();
            if (h != null) h.teamId = playerEconomy.TeamId;
            CreepPath p = creep.GetComponent<CreepPath>();
            if (p != null) p.currentWaypointIndex = (playerEconomy.TeamId % 4) + 1;
        }
    }

    void OnGUI()
    {
        DrawGameOver();
        if (showStatsPanel) DrawHeroStats();
    }

    void DrawGameOver()
    {
        string msg = null;
        if (Keeper.WinnerTeam != 0)
            msg = Keeper.WinnerTeam == PlayerClickMovement.LocalTeamId ? "ПОБЕДА!" : $"Победил игрок {Keeper.WinnerTeam}";
        else if (Keeper.IsEliminated(PlayerClickMovement.LocalTeamId))
            msg = "Ваш трон разрушен";
        if (msg == null) return;

        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        s.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 60), msg, s);
    }

    void DrawHeroStats()
    {
        PlayerClickMovement local = PlayerClickMovement.Local;
        if (local == null || local.Health == null || local.Attack == null) return;

        Health h = local.Health;
        AttackStats a = local.Attack.attack;

        string state = h.IsAlive
            ? $"HP: {h.currentHp:0} / {h.maxHp:0}  (+{h.hpRegen:0.#}/с)"
            : $"Мёртв, воскрешение через {h.RespawnTimeLeft:0.0} с";

        string text =
            $"{state}\n" +
            $"Урон: {a.damageMin:0}–{a.damageMax:0}  ({a.attackType})\n" +
            $"Скорость атаки: {a.AttacksPerSecond:0.00}/с   Дальность: {a.attackRange:0.#}\n" +
            $"Крит: {a.critChance * 100f:0}% x{a.critMultiplier:0.##}   Вампиризм: {a.lifesteal * 100f:0}%\n" +
            $"Броня: {h.armor:0.#} (−{h.ArmorReductionPercent:0}% физ.)   Маг. защита: {h.magicResistance * 100f:0}%\n" +
            $"Уклонение: {h.evasion * 100f:0}%   Скорость: {h.moveSpeed:0.#}";

        CreepSpawner spawner = myBarracks != null ? myBarracks.GetComponent<CreepSpawner>() : null;
        if (spawner != null) text += $"\nВолна {spawner.WaveNumber}, следующая через {spawner.NextWaveIn:0} с";

        GUIStyle box = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 13,
            padding = new RectOffset(8, 8, 6, 6)
        };
        box.normal.textColor = Color.white;
        GUI.Box(new Rect(10, Screen.height - 150, 400, 140), text, box);
    }
}
