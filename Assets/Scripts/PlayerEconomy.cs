using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Золото и доход игрока. Команда берётся из Health на этом же объекте (герой игрока).
public class PlayerEconomy : MonoBehaviour
{
    public int gold = 200;
    public int income = 20;
    public float incomeTimer = 15f;
    public TextMeshProUGUI timerText; // Ссылка на текст таймера

    static readonly Dictionary<int, PlayerEconomy> byTeam = new Dictionary<int, PlayerEconomy>();

    public static PlayerEconomy ForTeam(int teamId)
    {
        byTeam.TryGetValue(teamId, out PlayerEconomy eco);
        return eco;
    }

    public int TeamId { get; private set; } = 1;

    private float timer;

    void Awake()
    {
        Health h = GetComponent<Health>();
        if (h != null) TeamId = h.teamId;
        byTeam[TeamId] = this;
    }

    void OnDestroy()
    {
        if (byTeam.TryGetValue(TeamId, out PlayerEconomy eco) && eco == this) byTeam.Remove(TeamId);
    }

    void Start()
    {
        timer = incomeTimer;
    }

    void Update()
    {
        if (Keeper.IsEliminated(TeamId))
        {
            if (timerText != null) timerText.text = "Трон разрушен";
            return;
        }

        timer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Next Income: {Mathf.CeilToInt(timer)}s";
        }

        if (timer <= 0f)
        {
            AddGold(income);
            timer = incomeTimer;
        }
    }

    public void AddGold(int amount) => gold += amount;

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            return true;
        }
        return false;
    }
}
