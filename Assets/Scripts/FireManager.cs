using UnityEngine;
using UnityEngine.UI;

public class FireManager : MonoBehaviour
{
    [Header("Fire Effects")]
    public GameObject firePrefab; // Changed from VisualEffect to GameObject prefab
    public ParticleSystem sparkParticles;
    public ParticleSystem smokeParticles;
   
    [Header("Fire Audio")]
    public AudioSource fireAudioSource;
    public AudioClip fireLoopClip;
    public AudioClip extinguishClip;
    public AudioClip startFireClip;
    public AudioClip sparkClip;
    [Range(0f, 1f)]
    public float fireVolumeMax = 0.8f;
    [Range(0f, 1f)]
    public float sparkVolumeMax = 0.5f;
   
    [Header("Fire State")]
    public bool isFireActive = false;
    public bool isExtinguished = false;
   
    [Header("Extinguishing Settings")]
    public float extinguishTime = 5f;
    public float extinguishRadius = 10f;
    public string co2ParticleTag = "CO2";
   
    [Header("Visual Feedback")]
    public GameObject fireBaseIndicator;
    public Text statusText;

    [Header("Auto Ignition")]
    public bool enableAutoIgnition = true;
    public Vector2 autoIgnitionTimeRange = new Vector2(5f, 15f);
    
    [Header("Simple Scoring")]
    public static int totalScore = 0;
    public static float totalTime = 0f;
    public static int firesExtinguished = 0;
    public static float gameStartTime = 0f;
    
    // Private variables
    private float fireStartTime;
    private int thisFireScore = 0;
    private bool hasFireEverStarted = false;
    private float extinguishProgress = 0f;
    private float currentExtinguishRate = 0f;
    private bool isBeingExtinguished = false;
    private float startTime;
    private float endTime;
    private float initialFireVolume;
    private float autoIgnitionTimer = -1f;
    private bool isCompletelyExtinguished = false;
    
    // Fire prefab instance
    private GameObject fireInstance;
    
    // Events
    public System.Action<bool> OnFireStateChanged;
    public System.Action<float> OnExtinguishProgressChanged;
    public System.Action<string> OnStatusChanged;
   
    private GameObject co2System;
    private float lastCO2Contact;
    private float co2ContactTimeout = 0.5f;

    void Start()
    {
        InitializeFireEffects();
        InitializeAudio();
       
        if (fireBaseIndicator != null)
        {
            fireBaseIndicator.SetActive(false);
        }

        OnStatusChanged += UpdateStatusText;

        if (statusText != null)
        {
            statusText.text = GetCurrentStatus();
        }

        // Initialize game timer when first fire starts
        if (gameStartTime == 0f)
        {
            gameStartTime = Time.time;
        }

        // Set auto ignition timer if enabled
        if (enableAutoIgnition)
        {
            ResetAutoIgnitionTimer();
        }
    }

    void Update()
    {
        if (isFireActive && !isExtinguished)
        {
            CheckCO2Contact();
            UpdateExtinguishingProgress();
            UpdateFireAudio();
        }
        // FIXED: Simplified auto-ignition condition - removed isCompletelyExtinguished check that was preventing auto-ignition
        else if (enableAutoIgnition && !isFireActive && !isExtinguished)
        {
            // Auto ignition countdown
            if (autoIgnitionTimer > 0f)
            {
                autoIgnitionTimer -= Time.deltaTime;
                
                // Update status with countdown
                if (statusText != null)
                {
                    statusText.text = $"Sparks active - Fire may start in {autoIgnitionTimer:F0}s";
                }
                
                if (autoIgnitionTimer <= 0f)
                {
                    Debug.Log("Auto ignition triggered from sparks!");
                    StartFire();
                }
            }
        }
    }

    private void ResetAutoIgnitionTimer()
    {
        // FIXED: Removed isCompletelyExtinguished check that was preventing timer reset
        if (enableAutoIgnition)
        {
            autoIgnitionTimer = Random.Range(autoIgnitionTimeRange.x, autoIgnitionTimeRange.y);
            Debug.Log($"Auto ignition timer set to {autoIgnitionTimer:F1} seconds for {gameObject.name}");
        }
    }

    private void CalculateFireScore(float timeToExtinguish)
    {
        thisFireScore = 100; // Base score
        
        // Time bonus system
        if (timeToExtinguish <= 3f)
        {
            thisFireScore += 50; // Perfect bonus
        }
        else if (timeToExtinguish <= 7f)
        {
            thisFireScore += 25; // Good bonus
        }
        else if (timeToExtinguish > 15f)
        {
            // Penalty for very slow times
            int penalty = Mathf.RoundToInt((timeToExtinguish - 15f) * 5f);
            thisFireScore -= penalty;
            thisFireScore = Mathf.Max(20, thisFireScore); // Minimum 20 points
        }
        
        totalScore += thisFireScore;
        firesExtinguished++;
        
        Debug.Log($"Fire #{firesExtinguished} extinguished in {timeToExtinguish:F1}s - Score: {thisFireScore}");
    }

    private void CheckAllFiresExtinguished()
    {
        // Wait a moment to ensure all states are updated
        StartCoroutine(CheckAllFiresAfterDelay());
    }

    private System.Collections.IEnumerator CheckAllFiresAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        FireManager[] allFires = FindObjectsOfType<FireManager>();
        bool allFiresComplete = true;
        bool anyFireStarted = false;
        int activeFires = 0;
        int potentialFires = 0;
        
        Debug.Log($"Checking {allFires.Length} fires for completion...");
        
        foreach (FireManager fire in allFires)
        {
            Debug.Log($"Fire {fire.gameObject.name}: Started={fire.hasFireEverStarted}, Active={fire.isFireActive}, Extinguished={fire.isExtinguished}, CompletelyOut={fire.isCompletelyExtinguished}, AutoIgnition={fire.enableAutoIgnition}");
            
            // Count active fires
            if (fire.isFireActive && !fire.isExtinguished)
            {
                activeFires++;
                allFiresComplete = false;
            }
            
            // If this fire has ever started, it needs to be completely extinguished
            if (fire.hasFireEverStarted)
            {
                anyFireStarted = true;
                
                // Fire is NOT complete if it's not completely extinguished
                if (!fire.isCompletelyExtinguished)
                {
                    allFiresComplete = false;
                    Debug.Log($"Fire {fire.gameObject.name} is not completely extinguished yet");
                }
            }
            // FIXED: Check for potential fires that could still auto-ignite
            else if (fire.enableAutoIgnition && !fire.isCompletelyExtinguished)
            {
                potentialFires++;
                allFiresComplete = false;
                Debug.Log($"Fire {fire.gameObject.name} can still auto-ignite (timer: {fire.autoIgnitionTimer:F1}s)");
            }
        }
        
        Debug.Log($"Active fires: {activeFires}, Potential fires: {potentialFires}, All fires complete: {allFiresComplete}, Any started: {anyFireStarted}, Fires extinguished: {firesExtinguished}");
        
        // FIXED: Only show completion if there are no active fires AND no potential auto-ignition fires
        if (allFiresComplete && anyFireStarted && firesExtinguished > 0 && activeFires == 0 && potentialFires == 0)
        {
            totalTime = Time.time - gameStartTime;
            ShowFinalScore();
        }
    }

    private void ShowFinalScore()
    {
        string finalMessage = $"ALL FIRES COMPLETELY EXTINGUISHED!\n" +
                             $"Total Score: {totalScore} points\n" +
                             $"Total Time: {totalTime:F1} seconds\n" +
                             $"Fires Extinguished: {firesExtinguished}\n" +
                             $"Average Score: {(totalScore / firesExtinguished):F0} per fire\n" +
                             $"No more fire hazards remain!";
        
        if (statusText != null)
        {
            statusText.text = finalMessage;
            statusText.color = Color.blue;
            statusText.fontSize = 18;
        }
        
        Debug.Log("GAME COMPLETE: " + finalMessage);
        OnStatusChanged?.Invoke(finalMessage);
    }

    public void StartFire()
    {
        // Prevent starting if already completely extinguished
        if (isCompletelyExtinguished)
        {
            Debug.Log($"Cannot start fire on {gameObject.name} - completely extinguished");
            return;
        }
        
        fireStartTime = Time.time;
        thisFireScore = 0;
        hasFireEverStarted = true;
        autoIgnitionTimer = -1f; // Stop auto ignition timer
        
        Debug.Log($"Starting fire on {gameObject.name}");
        
        if (fireAudioSource != null)
        {
            fireAudioSource.Stop();
            if (startFireClip != null)
            {
                fireAudioSource.PlayOneShot(startFireClip);
            }
        }
       
        // Stop sparks and start fire effects
        if (sparkParticles != null) sparkParticles.Stop();
        
        // Instantiate fire prefab instead of using VisualEffect
        if (firePrefab != null && fireInstance == null)
        {
            fireInstance = Instantiate(firePrefab, transform.position, transform.rotation);
            fireInstance.transform.SetParent(transform); // Parent it to this object
            Debug.Log("Fire prefab instantiated and playing.");
        }
        else if (fireInstance != null)
        {
            fireInstance.SetActive(true);
            Debug.Log("Fire prefab activated.");
        }
        
        if (smokeParticles != null)
        {
            smokeParticles.Play();
            var emission = smokeParticles.emission;
            emission.rateOverTime = 15f;
            Debug.Log("Smoke particles playing.");
        }
        
        if (fireAudioSource != null && fireLoopClip != null)
        {
            fireAudioSource.clip = fireLoopClip;
            fireAudioSource.volume = initialFireVolume;
            fireAudioSource.Play();
            Debug.Log("Fire audio started.");
        }
        
        if (fireBaseIndicator != null)
        {
            fireBaseIndicator.SetActive(true);
            fireBaseIndicator.transform.position = transform.position;
        }
        
        isFireActive = true;
        startTime = Time.time;
        OnFireStateChanged?.Invoke(true);
        OnStatusChanged?.Invoke("FIRE DETECTED! Extinguish quickly for bonus points!");
        Debug.Log("Fire started! Use CO2 extinguisher to extinguish.");
    }

    private void CompleteExtinguishing()
    {
        isExtinguished = true;
        isBeingExtinguished = false;
        isFireActive = false;
        isCompletelyExtinguished = true; // Mark as completely done
        endTime = Time.time;
        float fireTime = endTime - fireStartTime;
        
        Debug.Log($"Completely extinguishing fire on {gameObject.name}");
        
        CalculateFireScore(fireTime);
        
        if (fireAudioSource != null)
        {
            fireAudioSource.Stop();
            if (extinguishClip != null)
            {
                fireAudioSource.PlayOneShot(extinguishClip);
            }
            Debug.Log("Fire audio stopped and extinguish sound played.");
        }
        
        // Deactivate fire prefab instead of stopping VisualEffect
        if (fireInstance != null)
        {
            fireInstance.SetActive(false);
            Debug.Log("Fire prefab deactivated.");
        }
        
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = 35f;
        }
        
        if (fireBaseIndicator != null)
            fireBaseIndicator.SetActive(false);
        
        // IMPORTANT: Stop sparks completely when fire is extinguished
        if (sparkParticles != null)
        {
            sparkParticles.Stop();
            Debug.Log("Sparks stopped - fire completely extinguished.");
        }
            
        Debug.Log($"Fire Extinguished Successfully in {fireTime:F1} seconds using CO2!");
        OnFireStateChanged?.Invoke(false);
        OnStatusChanged?.Invoke($"Fire extinguished! +{thisFireScore} points ({fireTime:F1}s)");
        
        // Check if all fires are now complete
        CheckAllFiresExtinguished();
        
        Invoke("ReduceSmoke", 3f);
        Invoke("StopSmoke", 8f);
    }

    public static void ResetGameStats()
    {
        totalScore = 0;
        totalTime = 0f;
        firesExtinguished = 0;
        gameStartTime = Time.time;
        Debug.Log("Game statistics reset!");
    }

    private void InitializeAudio()
    {
        if (fireAudioSource == null)
        {
            fireAudioSource = GetComponent<AudioSource>();
            if (fireAudioSource == null)
            {
                fireAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
       
        if (fireAudioSource != null)
        {
            fireAudioSource.loop = true;
            fireAudioSource.playOnAwake = false;
            fireAudioSource.volume = sparkVolumeMax;
            if (sparkClip != null)
            {
                fireAudioSource.clip = sparkClip;
                fireAudioSource.Play();
            }
            initialFireVolume = fireVolumeMax;
        }
    }

    private void UpdateFireAudio()
    {
        if (fireAudioSource != null && fireLoopClip != null)
        {
            float remainingIntensity = 1f - (extinguishProgress / extinguishTime);
            remainingIntensity = Mathf.Clamp01(remainingIntensity);
           
            fireAudioSource.volume = initialFireVolume * remainingIntensity;
           
            if (!fireAudioSource.isPlaying && isFireActive && !isExtinguished)
            {
                fireAudioSource.Play();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Object entered fire trigger: {other.name} with tag: {other.tag}");
        if (!isFireActive && !isExtinguished && !isCompletelyExtinguished && !other.name.Contains("CO2"))
        {
            Debug.Log("Starting fire due to trigger contact");
            StartFire();
        }
    }

    void OnParticleCollision(GameObject other)
    {
        Debug.Log($"Particle collision detected from: {other.name}");
      
        if (other.CompareTag(co2ParticleTag) || other.name.Contains("CO2"))
        {
            lastCO2Contact = Time.time;
            Debug.Log($"CO2 suppressing fire! Last contact: {lastCO2Contact}");
          
            if (!isBeingExtinguished && isFireActive && !isExtinguished)
            {
                StartExtinguishing();
            }
        }
    }

    void CheckCO2Contact()
    {
        if (co2System == null)
        {
            co2System = GameObject.FindGameObjectWithTag(co2ParticleTag);
            if (co2System == null)
            {
                co2System = GameObject.Find("CO2_Particles");
            }
        }
       
        if (co2System != null && isFireActive && !isExtinguished)
        {
            float distance = Vector3.Distance(co2System.transform.position, transform.position);
          
            ParticleSystem co2PS = co2System.GetComponent<ParticleSystem>();
            if (co2PS != null && co2PS.isPlaying && distance <= extinguishRadius)
            {
                lastCO2Contact = Time.time;
              
                if (!isBeingExtinguished)
                {
                    StartExtinguishing();
                }
            }
        }
       
        if (isBeingExtinguished && (Time.time - lastCO2Contact) > co2ContactTimeout)
        {
            StopExtinguishing();
        }
    }

    private void InitializeFireEffects()
    {
        // Make sure no fire instance exists at start
        if (fireInstance != null)
        {
            fireInstance.SetActive(false);
            Debug.Log("Fire prefab deactivated at start.");
        }
        
        if (sparkParticles != null)
        {
            sparkParticles.Play();
            Debug.Log("Sparks started at start.");
        }
        if (smokeParticles != null)
        {
            smokeParticles.Stop();
            Debug.Log("Smoke particles stopped at start.");
        }
    }

    private void StartExtinguishing()
    {
        isBeingExtinguished = true;
        currentExtinguishRate = 1f;
        Debug.Log("CO2 is suppressing the fire!");
        OnStatusChanged?.Invoke("CO2 Active! Keep spraying to extinguish the fire!");
    }

    private void StopExtinguishing()
    {
        isBeingExtinguished = false;
        currentExtinguishRate = 0f;
        Debug.Log("CO2 discharge stopped - fire may reignite if not fully extinguished.");
        OnStatusChanged?.Invoke("CO2 Stopped! Fire may grow stronger - keep spraying!");
    }

    private void UpdateExtinguishingProgress()
    {
        if (isBeingExtinguished)
        {
            extinguishProgress += currentExtinguishRate * Time.deltaTime;
           
            float remainingIntensity = 1f - (extinguishProgress / extinguishTime);
            remainingIntensity = Mathf.Clamp01(remainingIntensity);
           
            // Scale fire prefab instead of setting VisualEffect intensity
            if (fireInstance != null)
            {
                fireInstance.transform.localScale = Vector3.one * remainingIntensity;
            }
            
            if (smokeParticles != null)
            {
                var emission = smokeParticles.emission;
                emission.rateOverTime = Mathf.Lerp(15f, 40f, extinguishProgress / extinguishTime);
            }
            
            OnExtinguishProgressChanged?.Invoke(extinguishProgress / extinguishTime);
            
            if (extinguishProgress >= extinguishTime)
            {
                CompleteExtinguishing();
            }
        }
        else
        {
            if (extinguishProgress > 0f && extinguishProgress < extinguishTime)
            {
                extinguishProgress -= 0.2f * Time.deltaTime;
                extinguishProgress = Mathf.Max(0f, extinguishProgress);
               
                float reigniteIntensity = 1f - (extinguishProgress / extinguishTime);
                reigniteIntensity = Mathf.Clamp01(reigniteIntensity);
               
                // Scale fire prefab for reigniting effect
                if (fireInstance != null)
                {
                    fireInstance.transform.localScale = Vector3.one * reigniteIntensity;
                }
               
                OnExtinguishProgressChanged?.Invoke(extinguishProgress / extinguishTime);
            }
        }
    }

    private void ReduceSmoke()
    {
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = 10f;
        }
    }

    private void StopSmoke()
    {
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = 2f;
        }
        Invoke("CompleteSmokeClear", 5f);
    }

    private void CompleteSmokeClear()
    {
        if (smokeParticles != null)
        {
            smokeParticles.Stop();
            Debug.Log("All smoke effects stopped.");
        }
    }

    private void UpdateStatusText(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    public float GetExtinguishProgress()
    {
        return extinguishProgress / extinguishTime;
    }

    public bool IsBeingExtinguished()
    {
        return isBeingExtinguished;
    }

    public string GetCurrentStatus()
    {
        if (isCompletelyExtinguished) return "Fire completely extinguished - no hazard";
        if (enableAutoIgnition && autoIgnitionTimer > 0f && !hasFireEverStarted)
        {
            return $"Sparks active - Fire may start in {autoIgnitionTimer:F0}s";
        }
        if (!isFireActive && !isExtinguished) return "Sparks active - potential fire hazard";
        if (isExtinguished) return "Fire extinguished";
        if (isBeingExtinguished) return "Fire being suppressed by CO2";
        return "Fire active - use CO2 extinguisher!";
    }

    public void ResetFire()
    {
        isFireActive = false;
        isExtinguished = false;
        isBeingExtinguished = false;
        isCompletelyExtinguished = false; // FIXED: Allow fire to be reset and auto-ignite again
        extinguishProgress = 0f;
        currentExtinguishRate = 0f;
        startTime = 0f;
        endTime = 0f;
        hasFireEverStarted = false;
        
        // Reset fire prefab
        if (fireInstance != null)
        {
            fireInstance.SetActive(false);
            fireInstance.transform.localScale = Vector3.one; // Reset scale
        }
        
        if (smokeParticles != null)
        {
            smokeParticles.Stop();
            var emission = smokeParticles.emission;
            emission.rateOverTime = 15f;
        }
        if (sparkParticles != null)
        {
            sparkParticles.Play(); // Restart sparks
        }
        
        if (fireAudioSource != null)
        {
            fireAudioSource.Stop();
            if (sparkClip != null)
            {
                fireAudioSource.clip = sparkClip;
                fireAudioSource.volume = sparkVolumeMax;
                fireAudioSource.Play();
                Debug.Log("Audio reset to spark state.");
            }
        }
        
        if (fireBaseIndicator != null)
        {
            fireBaseIndicator.SetActive(false);
        }
        
        // Reset auto ignition if enabled
        if (enableAutoIgnition)
        {
            ResetAutoIgnitionTimer();
        }
        
        Debug.Log("Fire simulation reset to initial state with sparks.");
        OnFireStateChanged?.Invoke(false);
        OnStatusChanged?.Invoke("System reset. Sparks active - ready for fire trigger.");
    }

    public bool HasFireEverStarted()
    {
        return hasFireEverStarted;
    }

    public void ForceStartFire()
    {
        if (!isFireActive && !isExtinguished && !isCompletelyExtinguished)
        {
            StartFire();
        }
    }

    public void ForceExtinguish()
    {
        if (isFireActive && !isExtinguished)
        {
            extinguishProgress = extinguishTime;
            CompleteExtinguishing();
        }
    }

    void OnDestroy()
    {
        OnStatusChanged -= UpdateStatusText;
        
        // Clean up fire instance
        if (fireInstance != null)
        {
            DestroyImmediate(fireInstance);
        }
    }
}