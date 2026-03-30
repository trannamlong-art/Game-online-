using Fusion;
using UnityEngine;

public class RunnerSetup : MonoBehaviour
{
    void Awake()
    {
        var runner = GetComponent<NetworkRunner>();
        if (runner == null)
        {
            return;
        }

        runner.ProvideInput = true;

        var playerInputHandler = GetComponent<PlayerInputHandler>();
        var networkInputHandler = GetComponent<NetworkInputHandler>();

        // Keep exactly one active input callback source to avoid conflicting inputs.
        if (networkInputHandler != null)
        {
            networkInputHandler.enabled = false;
        }

        if (playerInputHandler != null)
        {
            runner.AddCallbacks(playerInputHandler);
        }
        else if (networkInputHandler != null)
        {
            networkInputHandler.enabled = true;
            runner.AddCallbacks(networkInputHandler);
        }
    }
}