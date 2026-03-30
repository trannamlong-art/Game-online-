using Fusion;
using UnityEngine;

public class EffectDestroy : NetworkBehaviour
{
    public float life = 1.5f;
    float timer;

    public override void Spawned()
    {
        timer = life;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        timer -= Runner.DeltaTime;

        if (timer <= 0f)
        {
            Runner.Despawn(Object);
        }
    }
}
