using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private CinemachineCamera CinemachineCamera;

    void Awake()
    {
        CinemachineCamera = GetComponent<CinemachineCamera>();
        if (CinemachineCamera == null)
        {
            Debug.LogError("CinemachineCamera component not found on the GameObject.");
        }
    }
    public void AssignCamera(Transform target)
    {
        if (CinemachineCamera != null)
        {
            CinemachineCamera.Follow = target;
            CinemachineCamera.LookAt = target;
        }
    }
}
