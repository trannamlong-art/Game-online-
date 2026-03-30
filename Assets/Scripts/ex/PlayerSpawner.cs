using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner == null || playerPrefab == null)
        {
            return;
        }

        bool shouldSpawn = false;

        // Host/Server modes: authoritative side spawns all players.
        if (Runner.IsServer)
        {
            shouldSpawn = true;
        }
        // Shared mode: each client spawns only its own player object.
        else if (Runner.GameMode == GameMode.Shared && player == Runner.LocalPlayer)
        {
            shouldSpawn = true;
        }

        if (!shouldSpawn)
        {
            return;
        }

        var obj = Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);

        var controller = obj.GetComponent<TankController>();
        if (controller != null && player == Runner.LocalPlayer)
        {
            controller.PlayerName = PlayerData.PlayerName;
        }
    }
}
