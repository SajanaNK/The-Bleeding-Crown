using UnityEngine;

namespace HeroKnightSandbox.Audio
{
    /// <summary>
    /// Per-scene background-music request, read by MusicPlayer on scene load. Pure data -
    /// MusicPlayer owns the actual AudioSource/playback.
    /// </summary>
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private float volume = 1f;

        public AudioClip Clip => clip;
        public float Volume => volume;
    }
}
