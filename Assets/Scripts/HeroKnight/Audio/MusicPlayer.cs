using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroKnightSandbox.Audio
{
    /// <summary>
    /// Persistent looping background-music AudioSource - one instance survives every scene
    /// change (same DontDestroyOnLoad/"first one built wins" convention as ScreenTransition;
    /// every scene's setup script builds one, see MusicSetup). On each scene load this
    /// instance adopts that scene's SceneMusic request, but only restarts playback if the
    /// clip actually changed - so Start Screen -> Level Select (same track) plays through
    /// without a restart/cut, and only switches when gameplay's different track loads.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
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
            // Applies the very first scene's request - every load after this one is instead
            // handled by HandleSceneLoaded below (mirrors ScreenTransition.Start()).
            if (Instance == this)
            {
                Apply(FindObjectOfType<SceneMusic>());
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply(FindObjectOfType<SceneMusic>());
        }

        private void Apply(SceneMusic request)
        {
            if (request == null || request.Clip == null)
            {
                return;
            }

            audioSource.volume = request.Volume;

            if (audioSource.clip == request.Clip)
            {
                return;
            }

            audioSource.clip = request.Clip;
            audioSource.Play();
        }
    }
}
