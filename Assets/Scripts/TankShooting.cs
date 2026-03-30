using Fusion;
using UnityEngine;

public class TankShooting : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed;
    int Team = 0;

    float firecooldown = 1.5f;
    float timer = 0f;

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<PlayerInputData>(out var input)) return;
        
        timer -= Runner.DeltaTime;

        if (input.isShooting && timer <= 0f)
        {
            Shoot();
            timer = firecooldown;
        }
    }

    void Shoot()
    {
        if (!Object.HasStateAuthority) return;

        var bullet = Runner.Spawn(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation,
            null, // ✅ KHÔNG có input authority
            (runner, obj) =>
            {
                obj.GetComponent<Bullet>().Team = Team;
            }
        );
    }
}