using HeroKnightSandbox.Audio;
using UnityEditor;
using UnityEngine;

namespace HeroKnightSandbox.EditorTools
{
/// <summary>
/// Builds this scene's background-music request (see SceneMusic) plus the persistent
/// MusicPlayer singleton that reads it - same "every scene builds one, first one built in
/// the play session survives" convention as ScreenTransitionSetup/ScreenTransition, so one
/// MusicPlayer carries playback across every scene change.
/// </summary>
public static class MusicSetup
{
    public const string StartingMusicPath = "Assets/Audio/music_starting.mp3";
    public const string GameplayMusicPath = "Assets/Audio/music_gameplay.mp3";

    // Menu screens run their music at full volume; gameplay's track plays underneath
    // combat/footstep/UI sfx, so it needs to sit well back in the mix.
    public const float GameplayVolume = 0.35f;

    public static void Build(string clipPath, float volume)
    {
        GameObject sceneMusicGO = new GameObject("SceneMusic");
        SceneMusic sceneMusic = sceneMusicGO.AddComponent<SceneMusic>();
        var so = new SerializedObject(sceneMusic);
        so.FindProperty("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        so.FindProperty("volume").floatValue = volume;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject musicPlayerGO = new GameObject("MusicPlayer");
        musicPlayerGO.AddComponent<AudioSource>();
        musicPlayerGO.AddComponent<MusicPlayer>();
    }
}
}
