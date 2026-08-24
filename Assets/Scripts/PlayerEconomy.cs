using UnityEngine;
using TMPro;

public class PlayerEconomy : MonoBehaviour
{
    public int gold = 200;
    public int income = 20;
    public float incomeTimer = 15f;
    public TextMeshProUGUI timerText; // Ссылка на текст таймера

    private float timer;

    void Start()
    {
        timer = incomeTimer;
    }

    void Update()
    {
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