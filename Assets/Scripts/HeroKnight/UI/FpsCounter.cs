using TMPro;
using UnityEngine;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// On-screen "FPS: NN" readout, averaged over updateInterval rather than shown
    /// per-frame (a raw 1/deltaTime reading jitters too fast to read). Uses
    /// unscaledDeltaTime so it keeps updating while paused (Time.timeScale 0).
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private float updateInterval = 0.5f;

        private TextMeshProUGUI text;
        private float timer;
        private int frames;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            frames++;
            timer += Time.unscaledDeltaTime;
            if (timer >= updateInterval)
            {
                int fps = Mathf.RoundToInt(frames / timer);
                text.text = "FPS: " + fps;
                frames = 0;
                timer = 0f;
            }
        }
    }
}
