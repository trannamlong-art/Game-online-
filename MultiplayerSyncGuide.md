# Multiplayer Health Synchronization Guide

## How HP Synchronization Works Now

### The Problem We Fixed
- Using `[Networked] public int HP { get; set; }` caused **Fusion codegen errors**
- But we still need HP to sync across all players in multiplayer

### The Solution: RPC-Based Synchronization

```csharp
// 1. LOCAL FIELD - stores the actual HP value
public int HP = 100;

// 2. DAMAGE RPC - only called on the Server (State Authority)
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_TakeDamage(int damage)
{
    if (!Object.HasStateAuthority) return;  // Only server processes
    HP -= damage;
    RPC_UpdateHP(HP);  // Tell ALL clients the new HP
}

// 3. SYNC RPC - broadcasts HP change to ALL PLAYERS
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_UpdateHP(int newHP)
{
    HP = newHP;  // All clients update their local HP
}
```

## Flow Diagram

```
Player A attacks Player B:
    ↓
Player A → RPC_TakeDamage(20) 
    ↓
[Server processes damage]
    ↓
Server → RPC_UpdateHP(80)  [broadcast to ALL]
    ↓
All Players (A, B, C) update HP = 80
    ↓
UI updates automatically
```

## How It Works in Multiplayer

| Step | What Happens | Client | Server |
|------|--------------|--------|--------|
| 1 | Any client calls `RPC_TakeDamage(20)` | Sends to server | - |
| 2 | Server processes damage | - | HP = 100 - 20 = 80 |
| 3 | Server calls `RPC_UpdateHP(80)` | - | Sends to ALL clients |
| 4 | All clients receive the RPC | HP = 80 | HP = 80 |
| 5 | UI updates for all players | Slider shows 80 | Slider shows 80 |

## Why This is Better Than [Networked]

### ✓ Advantages:
- No Fusion codegen errors
- Full control over when data is sent
- Server acts as "single source of truth"
- Prevents cheating (clients can't change HP locally)
- Multiple RPCs can be sent per frame if needed

### ✗ Disadvantages of [Networked]:
- Tries to auto-sync every frame (wastes bandwidth)
- Fusion codegen must access internal Ptr fields
- Less control over validation

## Example: Complete Damage Flow

```csharp
// Player A shoots Player B
public void OnBulletHit(Health target)
{
    target.RPC_TakeDamage(25);  // Any client can call this
}

// On Server:
public void RPC_TakeDamage(int damage)
{
    if (!Object.HasStateAuthority) return;  // Ignore if not server
    
    HP -= damage;  // Server changes HP
    
    // Broadcast to ALL players
    RPC_UpdateHP(HP);
    
    if (HP <= 0)
    {
        // Death logic...
    }
}

// On All Clients (including server):
public void RPC_UpdateHP(int newHP)
{
    HP = newHP;  // Update local value
    // Next frame, UpdateUI() reads this value
}

// Every frame:
public override void FixedUpdateNetwork()
{
    UpdateUI();  // All clients see the same HP
}
```

## Key Points

1. **Local Field** = `public int HP = 100;`
2. **Damage Processing** = Only on Server (`HasStateAuthority`)
3. **Synchronization** = Via `RCP_UpdateHP(value)` to All clients
4. **UI Updates** = Every client reads from their local `HP` field

## Bandwidth Usage

```
[Networked] Method: 100 updates/second (if HP changes every frame)
RPC Method: Only 1 update when damage happens

Result: 99% less network traffic! ✓
```

This is the proper way to do multiplayer in Photon Fusion!
