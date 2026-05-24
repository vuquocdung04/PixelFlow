using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitSpriteToScreen : MonoBehaviour
{
    [SerializeField] private float overscan = 1.02f;

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        float worldHeight = Camera.main.orthographicSize * 2f;
        float worldWidth = worldHeight * Camera.main.aspect;
        Vector2 spriteSize = sr.sprite.bounds.size;

        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y) * overscan;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}