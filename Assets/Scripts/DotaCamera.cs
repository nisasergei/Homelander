using UnityEngine;
using UnityEngine.InputSystem;

public class DotaCamera : MonoBehaviour
{
    public float panSpeed = 25f;         // Скорость движения (клавиатура и края экрана)
    public float edgeBoundary = 20f;     // Отступ в пикселях от края экрана для мыши
    public float rotateSpeed = 100f;     // Скорость вращения колесиком мыши
    public float zoomSpeed = 10f;        // Скорость зума
    public float minZoom = 10f;          // Минимальная высота
    public float maxZoom = 45f;          // Максимальная высота

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        // --- 1. ВРАЩЕНИЕ КАМЕРЫ (Зажатое колесико мыши MMB) ---
        if (Mouse.current.middleButton.isPressed)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            transform.Rotate(Vector3.up, mouseX * rotateSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            Vector3 moveDir = Vector3.zero;

            // --- 2. УПРАВЛЕНИЕ С КЛАВИАТУРЫ (WASD / Стрелки) ---
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveDir -= transform.right;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveDir += transform.right;
            }

            // Направление "вперед/назад" с учетом текущего поворота камеры
            Vector3 forward = transform.forward;
            forward.y = 0; // Игнорируем наклон вниз
            forward.Normalize();

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                moveDir += forward;
            }
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                moveDir -= forward;
            }

            // --- 3. ДВИЖЕНИЕ ПО КРАЯМ ЭКРАНА (МЫШЬ) ---
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (mousePos.x >= Screen.width - edgeBoundary) moveDir += transform.right;
            if (mousePos.x <= edgeBoundary) moveDir -= transform.right;
            if (mousePos.y >= Screen.height - edgeBoundary) moveDir += forward;
            if (mousePos.y <= edgeBoundary) moveDir -= forward;

            // Применяем движение (нормализуем, чтобы по диагонали не двигалось быстрее)
            if (moveDir != Vector3.zero)
            {
                transform.position += moveDir.normalized * panSpeed * Time.deltaTime;
            }
        }

        // --- 4. ЗУМ КОЛЕСИКОМ ---
        float scroll = Mouse.current.scroll.y.ReadValue();
        if (scroll != 0f)
        {
            float scrollAmount = scroll > 0 ? -1f : 1f;
            transform.position += transform.forward * scrollAmount * zoomSpeed * Time.deltaTime * 30f;

            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
            transform.position = pos;
        }
    }
}