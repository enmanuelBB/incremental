using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return; // si falta asignar un sonido, no rompe el juego
        audioSource.PlayOneShot(clip, volume);
    }
}
