using UnityEngine;

public class AmbientAudioStarter : MonoBehaviour
{
    [Tooltip("Name of the sound in AudioManager (must match the inspector name).")]
    [SerializeField] private string soundName;

    [Tooltip("Start after a short delay (optional).")]
    [SerializeField] private float delaySeconds = 0f;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(soundName))
        {
            Debug.LogWarning($"{nameof(AmbientAudioStarter)}: soundName is empty on {gameObject.name}");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(AmbientAudioStarter)}: AudioManager.Instance is null. Make sure AudioManager exists in the scene.");
            return;
        }

        if (delaySeconds > 0f)
            Invoke(nameof(PlayAmbient), delaySeconds);
        else
            PlayAmbient();
    }

    public void PlayAmbient()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.Play(soundName);
    }

    public void StopAmbient()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.Stop(soundName);
    }
}
