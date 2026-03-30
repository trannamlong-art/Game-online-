using UnityEngine;
using Fusion;

public class PlayerSetup : NetworkBehaviour
{
    public void SetupCamera(Transform playerTransform)
    {
        if(Object.HasStateAuthority)
        {
            CameraFollow cameraFollow = GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.AssignCamera(playerTransform);
            }
            else
            {
                Debug.LogError("CameraFollow component not found in the scene.");
            }
        }  
    }
}
