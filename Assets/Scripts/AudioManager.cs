using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        public bool loop = false;

        [HideInInspector]
        public AudioSource source;
    }

    [Header("Sounds")]
    public List<Sound> sounds = new();

    private Dictionary<string, Sound> soundMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        soundMap = new Dictionary<string, Sound>();

        foreach (var s in sounds)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = s.clip;
            source.volume = s.volume;
            source.pitch = s.pitch;
            source.loop = s.loop;
            source.playOnAwake = false;

            s.source = source;
            soundMap[s.name] = s;
        }
    }

    void Start()
    {
        var listeners = FindObjectsOfType<AudioListener>(true);
        Debug.Log("AudioListeners found: " + listeners.Length);
        foreach (var l in listeners)
            Debug.Log("Listener on: " + l.gameObject.name + " (enabled=" + l.enabled + ")");
    }

    public void Play(string name)
    {
        if (!soundMap.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"AudioManager: Sound '{name}' not found");
            return;
        }

        Debug.Log($"AudioManager.Play called with '{name}'");
        sound.source.Play();
    }

    public void Stop(string name)
    {
        if (!soundMap.TryGetValue(name, out var sound))
            return;

        sound.source.Stop();
    }

    public void SetVolume(string name, float volume)
    {
        if (!soundMap.TryGetValue(name, out var sound))
            return;

        sound.source.volume = volume;
    }

    public bool IsPlaying(string name)
    {
        return soundMap.TryGetValue(name, out var sound) && sound.source.isPlaying;
    }
}
