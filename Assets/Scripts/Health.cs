using Fusion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Health : NetworkBehaviour
{
    public GameObject deathEffect;

    // Regular fields for health tracking
    public int HP = 100;
    public int Mana = 30;

    [SerializeField] private int startHP = 100;
    [SerializeField] private int startMana = 30;

    public int Team;

    public Slider healthSlider;
    public Slider manaSlider;

    private PlayerInput playerInput;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            HP = startHP;
            Mana = startMana;
        }

        playerInput = GetComponent<PlayerInput>();
        UpdateUI();
    }

    public override void FixedUpdateNetwork()
    {
        // 🔥 Just update UI - don't call RPCs! They cause InvokeRpc errors
        UpdateUI();
        
        // 🔥 Host handles death state
        if (Object.HasStateAuthority && HP <= 0)
        {
            if (deathEffect != null)
            {
                Runner.Spawn(deathEffect, transform.position, Quaternion.identity);
            }
            Runner.Despawn(Object);
        }
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = 100;
            healthSlider.value = HP;
        }

        if (manaSlider != null)
        {
            manaSlider.maxValue = 30;
            manaSlider.value = Mana;
        }
    }

    // 🔥 Take damage - just modify HP directly
    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        
        HP -= damage;
        HP = Mathf.Max(0, HP);
        
        Debug.Log($"🔥 HP: {HP}");
    }
}