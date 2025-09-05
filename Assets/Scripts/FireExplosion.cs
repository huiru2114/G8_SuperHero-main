using UnityEngine;

public class FireExplosion : MonoBehaviour
{
    [Header("Effects")]
    public GameObject sparkEffect;       // Spark (assign in Inspector)
    public GameObject explosionEffect;   // Explosion prefab
    public GameObject fireEffect;        // Fire prefab (optional)

    [Header("Spawn Points (optional)")]
    public Transform explosionSpawnPoint; // Where explosion should appear
    public Transform fireSpawnPoint;      // Where fire should appear

    [Header("Timing")]
    public float delayBeforeExplosion = 5f;

    void Start()
    {
        Invoke(nameof(TriggerExplosion), delayBeforeExplosion);
    }

    void TriggerExplosion()
    {
        // Disable spark effect
        if (sparkEffect != null)
            sparkEffect.SetActive(false);

        // Spawn explosion
        if (explosionEffect != null)
        {
            Vector3 pos = explosionSpawnPoint ? explosionSpawnPoint.position : transform.position;
            Instantiate(explosionEffect, pos, Quaternion.identity);
        }

        // Spawn fire
        if (fireEffect != null)
        {
            Vector3 pos = fireSpawnPoint ? fireSpawnPoint.position : transform.position;
            Instantiate(fireEffect, pos, Quaternion.identity);
        }
    }
}
