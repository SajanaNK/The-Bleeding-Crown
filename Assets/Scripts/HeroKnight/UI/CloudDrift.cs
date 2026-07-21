using UnityEngine;

namespace HeroKnightSandbox.UI
{
/// <summary>
/// Slowly drifts a UI element horizontally, wrapping back around once it strays
/// further than rangeX from its starting position. Purely cosmetic parallax for
/// background elements like the Start Screen's clouds.
/// </summary>
public class CloudDrift : MonoBehaviour
{
    public float speed = 6f;
    public float rangeX = 700f;

    private RectTransform rectTransform;
    private float startX;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        startX = rectTransform.anchoredPosition.x;
    }

    private void Update()
    {
        float x = rectTransform.anchoredPosition.x + speed * Time.deltaTime;
        if (x > startX + rangeX)
        {
            x -= rangeX * 2f;
        }
        else if (x < startX - rangeX)
        {
            x += rangeX * 2f;
        }
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
    }
}
}
