using UnityEngine;

namespace HeroKnightSandbox.UI
{
/// <summary>
/// Gently rocks a UI element's rotation back and forth around its own pivot, like a
/// tree or bush swaying in wind. Pair with a bottom-center pivot so it rocks from its
/// base rather than its middle.
/// </summary>
public class WindSway : MonoBehaviour
{
    public float swayAngle = 4f;
    public float swaySpeed = 1f;

    private RectTransform rectTransform;
    private float phase;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float angle = Mathf.Sin(Time.time * swaySpeed + phase) * swayAngle;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
}
