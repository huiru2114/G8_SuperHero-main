using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AlarmSound : MonoBehaviour
{
    [Header("Alarm Settings")]
    public AudioClip alarmClip;
    [Range(0f, 1f)] public float alarmVolume = 1f;

    private AudioSource audioSource;
    private int activeFireCount = 0; // Track number of active fires

    void Awake()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = alarmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = alarmVolume;
    }

    void OnEnable()
    {
        // Subscribe to fire events
        FireManager[] allFires = FindObjectsOfType<FireManager>();
        foreach (FireManager fire in allFires)
        {
            fire.OnFireStateChanged += HandleFireStateChanged;

            // Check if fire is already burning when game starts
            if (fire.isFireActive)
                activeFireCount++;
        }

        // If any fires already active at start, play alarm
        if (activeFireCount > 0)
            PlayAlarm();
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        FireManager[] allFires = FindObjectsOfType<FireManager>();
        foreach (FireManager fire in allFires)
        {
            fire.OnFireStateChanged -= HandleFireStateChanged;
        }
    }

    private void HandleFireStateChanged(bool isFireActive)
    {
        if (isFireActive)
        {
            activeFireCount++;
            if (!audioSource.isPlaying)
                PlayAlarm();
        }
        else
        {
            activeFireCount = Mathf.Max(0, activeFireCount - 1);
            if (activeFireCount == 0)
                StopAlarm();
        }
    }

    public void PlayAlarm()
    {
        if (!audioSource.isPlaying && alarmClip != null)
        {
            audioSource.Play();
            Debug.Log("🚨 Alarm started!");
        }
    }

    public void StopAlarm()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("🚨 Alarm stopped!");
        }
    }
}
