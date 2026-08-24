using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health healthScript;
    public Image fillImage;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (healthScript == null) healthScript = GetComponentInParent<Health>();
    }

    void Update()
    {
        if (healthScript == null || fillImage == null) return;

        // Обновляем шкалу HP
        fillImage.fillAmount = healthScript.currentHp / healthScript.maxHp;

        // Поворачиваем Canvas с HP bar всегда ликом к камере
        if (mainCam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
        }
    }
}