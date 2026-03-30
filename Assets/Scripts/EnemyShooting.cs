using UnityEngine;
using Fusion;

public class EnemyShooting : NetworkBehaviour
{
    public int Team { get; set; }
    public GameObject bulletPrefab;
    public Transform firePointLeft;
    public Transform firePointRight;
    public float bulletSpeed = 10f;
    public float attackRange = 15f;
    public float fireRate = 2f;
    public float searchRange = 20f;

    float fireCooldown;
    GameObject cachedTarget = null;
    float targetSearchCooldown = 0.5f;
    float timeSinceLastSearch = 0f;
    
    void Start()
    {
        Team = 1;
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Update target search with cooldown
        timeSinceLastSearch += Runner.DeltaTime;
        if (timeSinceLastSearch >= targetSearchCooldown)
        {
            cachedTarget = GetNearestPlayer();
            timeSinceLastSearch = 0f;
        }

        fireCooldown -= Runner.DeltaTime;

        if (cachedTarget == null) return;

        // Check if target still exists and within range
        float distance = Vector3.Distance(transform.position, cachedTarget.transform.position);

        if (distance <= attackRange)
        {
            RotateToTarget(cachedTarget);

            if (fireCooldown <= 0f)
            {
                Shoot();
                fireCooldown = fireRate;
            }
        }
    }

    GameObject GetNearestPlayer()
    {
        // Find all objects with "Player" tag
        var players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return null;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            if (p == null) continue;
            
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist && dist <= searchRange)
            {
                minDist = dist;
                nearest = p;
            }
        }

        return nearest;
    }

    void RotateToTarget(GameObject target)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                Runner.DeltaTime * 5f
            );
        }
    }

    void Shoot()
    {
        if (!Object.HasStateAuthority) return;
        int shooterTeam = Team;

        // 🔥 LEFT
        Runner.Spawn(
            bulletPrefab,
            firePointLeft.position,
            firePointLeft.rotation,
            null,
            (runner, obj) =>
            {
                var bullet = obj.GetComponent<Bullet>();
                bullet.Team = shooterTeam;
                bullet.speed = bulletSpeed;  // 🔥 SET SPEED!
            }
        );

        // 🔥 RIGHT
        Runner.Spawn(
            bulletPrefab,
            firePointRight.position,
            firePointRight.rotation,
            null,
            (runner, obj) =>
            {
                var bullet = obj.GetComponent<Bullet>();
                bullet.Team = shooterTeam;
                bullet.speed = bulletSpeed;  // 🔥 SET SPEED!
            }
        );
    }
}