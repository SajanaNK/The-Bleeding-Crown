using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
/// <summary>
/// A minimal flapping "V" chevron built from two thin bars, standing in for a bird
/// silhouette since no bird sprite exists in the project's assets. Drifts horizontally
/// with wraparound, bobs vertically, and flaps its two "wings" around a shared pivot.
/// </summary>
public class FlyingBird : MonoBehaviour
{
    public float driftSpeed = 50f;
    public float driftRangeX = 900f;
    public float bobAmplitude = 15f;
    public float bobSpeed = 1.2f;
    public float flapSpeed = 6f;
    public float flapAngle = 25f;

    private RectTransform rootRT;
    private RectTransform leftWing;
    private RectTransform rightWing;
    private float startX;
    private float startY;
    private float bobPhase;

    private void Awake()
    {
        rootRT = (RectTransform)transform;
        startX = rootRT.anchoredPosition.x;
        startY = rootRT.anchoredPosition.y;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        leftWing = CreateWing("LeftWing", new Vector2(1f, 0.5f));
        rightWing = CreateWing("RightWing", new Vector2(0f, 0.5f));
    }

    private RectTransform CreateWing(string wingName, Vector2 pivot)
    {
        GameObject wingGO = new GameObject(wingName);
        wingGO.transform.SetParent(rootRT, false);
        RectTransform rt = wingGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(14f, 3f);
        rt.anchoredPosition = Vector2.zero;

        Image image = wingGO.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        image.raycastTarget = false;

        return rt;
    }

    private void Update()
    {
        float x = rootRT.anchoredPosition.x + driftSpeed * Time.deltaTime;
        if (driftSpeed > 0f && x > startX + driftRangeX)
        {
            x -= driftRangeX * 2f;
        }
        else if (driftSpeed < 0f && x < startX - driftRangeX)
        {
            x += driftRangeX * 2f;
        }
        float y = startY + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
        rootRT.anchoredPosition = new Vector2(x, y);

        float flap = Mathf.Sin(Time.time * flapSpeed) * flapAngle;
        leftWing.localEulerAngles = new Vector3(0f, 0f, 25f + flap);
        rightWing.localEulerAngles = new Vector3(0f, 0f, -25f - flap);
    }
}
}
