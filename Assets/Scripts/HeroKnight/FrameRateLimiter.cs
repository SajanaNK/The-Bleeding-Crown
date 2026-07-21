using UnityEngine;

namespace HeroKnightSandbox
{
    /// <summary>
    /// Caps the framerate once per play session, before any scene loads - no GameObject
    /// or per-scene wiring needed. QualitySettings.vSyncCount must be 0 for
    /// Application.targetFrameRate to have any effect - Unity ignores targetFrameRate
    /// entirely whenever vSync is on, which is the default on most of this project's
    /// quality tiers (see QualitySettings.asset).
    /// </summary>
    internal static class FrameRateLimiter
    {
        private const int TargetFrameRate = 200;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
