using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;
    public void PlayerJoined(PlayerRef player)
    {
        if(player==Runner.LocalPlayer)
        {
            Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }
   
}
