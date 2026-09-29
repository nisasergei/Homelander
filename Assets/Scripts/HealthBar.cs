using UnityEngine;
using UnityEngine.UI;

// Полоска HP над юнитом. Цвет: зелёный — свои, красный — враги.
// Health сам создаёт полоску из префаба (поле healthBarPrefab) и вызывает Setup.
// Размеры задаются здесь в мировых единицах — разметка префаба игнорируется,
// поэтому полоска не раздувается, как бы ни был настроен WorldHealthBar.prefab.
public class HealthBar : MonoBehaviour
{
    public Health healthScript;
    public Image fillImage;
    [Tooltip("Прятать полоску у крипов с полным HP, чтобы не рябило")]
    public bool hideCreepBarWhenFull = true;

    [Header("Размер (в метрах)")]
    public float barWidth = 1.2f;
    public float barHeight = 0.15f;
    [Tooltip("Толщина тёмной рамки вокруг заливки")]
    public float border = 0.02f;

    public Color allyColor = new Color(0.1f, 0.75f, 0.15f);
    public Color enemyColor = new Color(0.85f, 0.15f, 0.12f);
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

    private Camera mainCam;
    private Canvas canvas;
    private RectTransform fillRect;
    private float sizeMultiplier = 1f;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.renderMode = RenderMode.WorldSpace;
        
        GraphicRaycaster gr = GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        ApplyLayout();
    }
    
    public void Setup(Health owner, float scale)
    {
        healthScript = owner;
        sizeMultiplier = Mathf.Max(0.1f, scale);
        ApplyLayout();
    }

    void Start()
    {
        mainCam = Camera.main;
        if (healthScript == null) healthScript = GetComponentInParent<Health>();
        
        if (healthScript == null)
        {
            if (canvas != null) canvas.enabled = false;
            enabled = false;
            return;
        }

        if (fillImage != null)
            fillImage.color = healthScript.teamId == PlayerClickMovement.LocalTeamId ? allyColor : enemyColor;
    }

    // Приводит префаб к нормальным размерам: корень = barWidth x barHeight,
    // фон растянут на весь корень, заливка растянута на фон с отступом border.
    void ApplyLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null) return;
        
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        root.localScale = new Vector3(
            1f / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
            1f / Mathf.Max(0.01f, Mathf.Abs(parentScale.z)));
        root.sizeDelta = new Vector2(barWidth, barHeight) * sizeMultiplier;
        root.pivot = new Vector2(0.5f, 0.5f);

        if (fillImage == null) return;
        fillRect = fillImage.rectTransform;
        RectTransform bg = fillRect.parent as RectTransform;
        if (bg != null && bg != root)
        {
            Stretch(bg, 0f);
            Image bgImage = bg.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = backgroundColor;
                bgImage.type = Image.Type.Simple;
            }
        }
        
        fillImage.type = Image.Type.Simple;
        Stretch(fillRect, border * sizeMultiplier);
        fillRect.pivot = new Vector2(0f, 0.5f);
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    void LateUpdate()
    {
        if (healthScript == null || fillRect == null) return;

        bool visible = healthScript.IsAlive;
        if (visible && hideCreepBarWhenFull && healthScript.unitKind == UnitKind.Creep && healthScript.HpPercent >= 0.999f)
            visible = false;
        if (canvas != null) canvas.enabled = visible;
        if (!visible) return;
        
        Vector2 max = fillRect.anchorMax;
        max.x = healthScript.HpPercent;
        fillRect.anchorMax = max;
        
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }
}
