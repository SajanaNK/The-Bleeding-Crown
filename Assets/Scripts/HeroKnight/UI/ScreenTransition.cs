using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Fades a full-screen black Image out before loading a scene and back in once the
    /// new scene has settled, via LoadScene() below. Lives on a DontDestroyOnLoad root
    /// object so one instance survives every scene change; every scene's setup script
    /// builds one (see ScreenTransitionSetup), but only the first one built in a given
    /// play session survives - later scenes' copies find Instance already set and
    /// self-destruct in Awake.
    /// </summary>
    public class ScreenTransition : MonoBehaviour
    {
        public static ScreenTransition Instance { get; private set; }

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.35f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            // Fades in the very first scene of the session - every load after this one
            // is instead handled by HandleSceneLoaded below.
            if (Instance == this)
            {
                StartCoroutine(Fade(1f, 0f));
            }
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(FadeOutThenLoad(sceneName));
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(Fade(1f, 0f));
        }

        private IEnumerator FadeOutThenLoad(string sceneName)
        {
            yield return StartCoroutine(Fade(0f, 1f));
            // Time.timeScale persists across scene loads, so a load triggered from a
            // paused state (timeScale 0) would otherwise leave the next scene frozen.
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float from, float to)
        {
            // Blocks clicks to whatever's underneath while fading out/opaque, and lets
            // them back through once fully faded in.
            fadeImage.raycastTarget = to > 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                // Unscaled so the fade still plays out when triggered from a paused
                // (Time.timeScale 0) state, eg the Pause menu's "Quit to Start Screen".
                t += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / fadeDuration));
                yield return null;
            }
            fadeImage.color = new Color(0f, 0f, 0f, to);
        }
    }
}
