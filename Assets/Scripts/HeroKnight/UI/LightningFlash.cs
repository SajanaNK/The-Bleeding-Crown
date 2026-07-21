using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
/// <summary>
/// Drives a full-screen white Image through random double-flashes to simulate distant
/// lightning - the Start Screen's "storm about to break" mood. Visual only; there's no
/// thunder sound in the project's audio assets to pair with it.
/// </summary>
[RequireComponent(typeof(Image))]
public class LightningFlash : MonoBehaviour
{
    public float minInterval = 4f;
    public float maxInterval = 10f;
    public float peakAlpha = 0.55f;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnEnable()
    {
        StartCoroutine(FlashLoop());
    }

    private IEnumerator FlashLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            yield return StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        yield return StartCoroutine(FadeTo(peakAlpha, 0.05f));
        yield return StartCoroutine(FadeTo(0.1f, 0.1f));
        yield return StartCoroutine(FadeTo(peakAlpha * 0.7f, 0.05f));
        yield return StartCoroutine(FadeTo(0f, 0.4f));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = image.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            image.color = new Color(1f, 1f, 1f, Mathf.Lerp(startAlpha, targetAlpha, t / duration));
            yield return null;
        }
        image.color = new Color(1f, 1f, 1f, targetAlpha);
    }
}
}
