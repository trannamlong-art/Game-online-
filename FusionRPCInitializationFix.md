# Fusion RPC Initialization Fix Guide

## The Problem

```
FieldAccessException: Field `Fusion.NetworkBehaviour:InvokeRpc' is inaccessible
```

**When:** Calling RPCs too early before `Spawned()` is called
**Why:** The NetworkObject's RPC infrastructure isn't initialized yet

## The Solution

### ✓ Correct Timeline

```
1. Runner.Spawn() called
          ↓
2. Network object created (but NOT ready for RPCs yet)
          ↓
3. Spawned() called ← Object is NOW fully initialized
          ↓
4. RPCs can now be safely called ✓
```

### ✗ Wrong Approach

```csharp
// WRONG - Called during PlayerJoined (before Spawned):
var obj = Runner.Spawn(playerPrefab, ...);
var controller = obj.GetComponent<TankController>();
controller.SetName(name);  // ❌ InvokeRpc not ready yet!
```

### ✓ Correct Approach

```csharp
// Step 1: In PlayerSpawner - just set the field
var obj = Runner.Spawn(playerPrefab, ...);
var controller = obj.GetComponent<TankController>();
controller.PlayerName = PlayerData.PlayerName;  // ✓ Direct assignment

// Step 2: In TankController.Spawned() - NOW call RPC
public override void Spawned()
{
    if (Object.HasInputAuthority && !string.IsNullOrEmpty(PlayerName))
    {
        RPC_SetName(PlayerName);  // ✓ Now safe to call RPC!
    }
}

// Step 3: RPC broadcasts to all other players
[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
public void RPC_SetName(string name)
{
    PlayerName = name;
}
```

## Complete Fixed Code

### PlayerSpawner.cs
```csharp
public void PlayerJoined(PlayerRef player)
{
    if (player == Runner.LocalPlayer)
    {
        var obj = Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        var controller = obj.GetComponent<TankController>();
        
        // Set directly - no RPC yet
        controller.PlayerName = PlayerData.PlayerName;
    }
}
```

### TankController.cs
```csharp
// Regular field (not [Networked])
public string PlayerName = "";

// Called manually after Spawned()
public void SetName(string name)
{
    PlayerName = name;
    if (Object != null && Object.HasInputAuthority && Runner != null)
    {
        RPC_SetName(name);  // Only call if object is initialized
    }
}

// Called during initialization
public override void Spawned()
{
    // ... other setup ...
    
    if (Object.HasInputAuthority)
    {
        // Now it's safe to call RPC
        if (!string.IsNullOrEmpty(PlayerName))
        {
            RPC_SetName(PlayerName);
        }
    }
}

// Broadcasts to all clients
[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
public void RPC_SetName(string name)
{
    PlayerName = name;
}
```

### Health.cs
```csharp
// Regular fields
public int HP = 100;
public int Mana = 30;

// Damage call from any player
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_TakeDamage(int damage)
{
    if (!Object.HasStateAuthority) return;
    HP -= damage;
    RPC_UpdateHP(HP);  // Safe - called from StateAuthority
}

// Broadcast to all players
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_UpdateHP(int newHP)
{
    HP = newHP;
}
```

## The Lifecycle

```
Phase 1: Spawn (PlayerSpawner)
├─ Runner.Spawn() → Object created
├─ Set fields directly: controller.PlayerName = name
└─ ❌ DO NOT call RPCs here

Phase 2: Spawned() (TankController)
├─ Spawned() callback fires
├─ ✓ NOW RPCs are ready!
├─ Call RPC: RPC_SetName(PlayerName)
└─ ✓ Other clients receive the synchronization

Phase 3: Runtime
├─ RPC_TakeDamage() called by any client
├─ Server processes: HP -= damage
├─ Server broadcasts: RPC_UpdateHP(HP)
└─ All clients update: HP = newHP
```

## Key Rules

✓ **Set fields directly** during Spawn (PlayerJoined)
✓ **Call RPCs** during or after Spawned()
✓ **Check Object.HasInputAuthority** before calling RPC
✓ **Check Runner != null** before using Runner

❌ **DON'T** call RPCs before Spawned()
❌ **DON'T** assume Object is ready immediately after Spawn()
❌ **DON'T** use [Networked] properties (use regular fields + RPCs)

## Result

✓ No more `InvokeRpc` FieldAccessException
✓ Network synchronization works correctly
✓ Code is more explicit and debuggable
✓ Matches Photon Fusion best practices
