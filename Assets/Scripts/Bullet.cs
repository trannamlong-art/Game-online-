using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public int Team { get; set; }

    float lifeTime = 3f;
    public GameObject effect;
    public float speed = 10f;  // 🔥 Default speed (gets overwritten by spawn calls)
    Vector3 prevPos;

    public override void Spawned()
    {
        prevPos = transform.position;
        
        // 🔥 CRITICAL: Ensure bullet is KINEMATIC and TRIGGER to avoid recoil/bouncing
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;           // NO physics simulation
            // ⚠️ Don't set linearVelocity on kinematic bodies - it's not supported!
            // Bullet movement is handled via transform.position in FixedUpdateNetwork
        }

        // 🔥 Ensure collider is TRIGGER (passes through objects, no bouncing)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        Debug.Log($"🔥 Bullet spawned! Speed: {speed}, Effect: {(effect != null ? effect.name : "NULL")}");
    }

    public override void FixedUpdateNetwork()
    {
        // 🔥 CHỈ host điều khiển
        if (Object.HasStateAuthority)
        {
            // Move bullet straight forward (no gravity)
            transform.position += transform.forward * speed * Runner.DeltaTime;

            lifeTime -= Runner.DeltaTime;
            if (lifeTime <= 0f)
            {
                Runner.Despawn(Object);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        var hit = other.GetComponent<Health>();

        if (other.CompareTag("Wall"))
        {
            SpawnEffect();
            Runner.Despawn(Object);
            return;
        }

        if (hit != null)
        {
            if (hit.Team == Team)
                return;

            // 🔥 Apply damage
            hit.TakeDamage(10);
            SpawnEffect();
            Runner.Despawn(Object);
        }
    }

    void SpawnEffect()
    {
        if (effect != null)
        {
            var spawnedEffect = Runner.Spawn(effect, transform.position, Quaternion.identity);
            Debug.Log($"🔥 Effect spawned at {transform.position}");
        }
        else
        {
            Debug.LogWarning("⚠️ Bullet.effect is NULL!");
        }
    }
}