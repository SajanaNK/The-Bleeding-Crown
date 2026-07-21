using UnityEngine;

namespace HeroKnightSandbox.Audio
{
    /// <summary>
    /// Picks a random clip from a set and fires it via PlayOneShot (not Play) - so rapid
    /// re-triggers (combo attacks, footsteps) layer instead of cutting each other off.
    /// </summary>
    public static class RandomAudioPlayer
    {
        public static void Play(AudioSource source, AudioClip[] clips)
        {
            if (source == null || clips == null || clips.Length == 0)
            {
                return;
            }

            source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        /// <summary>
        /// Same as Play(), but cancels whatever this source is currently playing first -
        /// for sounds that represent one ongoing/repeating action (footsteps, block,
        /// attack swings, jump) where only one instance should ever be audible, rather
        /// than layering. Deliberately does NOT use PlayOneShot: those voices are
        /// documented as fire-and-forget once started - not stoppable, not queryable via
        /// isPlaying, immune to a later Stop() call on the same source. Assigning .clip
        /// and calling Play() instead uses the source's one controllable primary slot,
        /// which Play() always cuts over to immediately, guaranteeing exclusivity.
        /// </summary>
        public static void PlayExclusive(AudioSource source, AudioClip[] clips)
        {
            if (source == null || clips == null || clips.Length == 0)
            {
                return;
            }

            source.clip = clips[Random.Range(0, clips.Length)];
            source.Play();
        }
    }
}
