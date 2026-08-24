using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public PlayerEconomy playerEconomy;
    public Transform myBarracks;      // Ссылка на Barracks_P1
    public GameObject creepPrefab;    // Префаб крипа
    public TextMeshProUGUI goldText;  // Текст золота в UI

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
        int cost = 50;
        int incomeBonus = 10;

        if (playerEconomy.SpendGold(cost))
        {
            // Увеличиваем доход
            playerEconomy.income += incomeBonus;

            // Спавним купленного крипа возле своего Барака
            if (creepPrefab != null && myBarracks != null)
            {
                Instantiate(creepPrefab, myBarracks.position + Vector3.forward * 2f, Quaternion.identity);
            }
        }
    }
}