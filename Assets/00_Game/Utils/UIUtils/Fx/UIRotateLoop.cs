using UnityEngine;

// Xoay liên tục (tia sáng, vòng sáng...). Dùng thời gian thực để vẫn chạy khi game pause.
public class UIRotateLoop : MonoBehaviour
{
    [SerializeField] private float speed = 30f;

    private void Update() => transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
}
