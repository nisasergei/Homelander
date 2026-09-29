using System.Collections.Generic;
using UnityEngine;

// Всплывающие цифры урона над юнитами. Создается автоматически, в сцену добавлять не нужно.
// Желтые - урон героя, белые - урон крипов, оранжевые крупные - криты, золотые - награда.
public class DamagePopups : MonoBehaviour
{
    public static bool Enabled = true;

    struct Popup
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public int fontSize;
        public float bornAt;
    }

    const float Lifetime = 0.9f;
    const float RiseSpeed = 1.6f;

    static DamagePopups instance;
    readonly List<Popup> popups = new List<Popup>();
    GUIStyle style;
    Camera cam;

    static DamagePopups Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[DamagePopups]");
                instance = go.AddComponent<DamagePopups>();
            }
            return instance;
        }
    }

    public static void ShowDamage(Health target, float amount, DamageInfo info)
    {
        if (!Enabled || target == null) return;

        Color color = Color.white;
        if (info.source != null && info.source.unitKind == UnitKind.Hero) color = new Color(1f, 0.85f, 0.2f);
        if (info.damageType == DamageType.Magical) color = new Color(0.45f, 0.7f, 1f);
        if (info.damageType == DamageType.Pure) color = new Color(1f, 0.95f, 0.95f);

        string text = Mathf.RoundToInt(amount).ToString();
        int size = 15;
        if (info.isCrit)
        {
            color = new Color(1f, 0.35f, 0.1f);
            text += "!";
            size = 22;
        }
        Instance.Add(target, text, color, size);
    }

    public static void ShowMiss(Health target)
    {
        if (!Enabled || target == null) return;
        Instance.Add(target, "промах", new Color(0.8f, 0.8f, 0.8f), 13);
    }

    public static void ShowGold(Health target, int gold)
    {
        if (!Enabled || target == null) return;
        Instance.Add(target, $"+{gold}g", new Color(1f, 0.8f, 0f), 17);
    }

    void Add(Health target, string text, Color color, int size)
    {
        Vector3 pos = target.transform.position + Vector3.up * 1.8f;
        pos += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        popups.Add(new Popup { worldPos = pos, text = text, color = color, fontSize = size, bornAt = Time.time });
    }

    void OnGUI()
    {
        if (popups.Count == 0) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            Popup p = popups[i];
            float age = Time.time - p.bornAt;
            if (age > Lifetime)
            {
                popups.RemoveAt(i);
                continue;
            }

            Vector3 screen = cam.WorldToScreenPoint(p.worldPos + Vector3.up * age * RiseSpeed);
            if (screen.z < 0f) continue;

            float alpha = 1f - Mathf.Clamp01((age - Lifetime * 0.5f) / (Lifetime * 0.5f));
            Rect r = new Rect(screen.x - 50f, Screen.height - screen.y - 12f, 100f, 24f);

            style.fontSize = p.fontSize;
            style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), p.text, style);
            style.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, alpha);
            GUI.Label(r, p.text, style);
        }
    }
}
