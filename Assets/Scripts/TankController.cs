using Fusion;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankController : NetworkBehaviour
{
    public Transform lowBody;
    public Transform turret;
    public CinemachineCamera cam;
    [SerializeField] private GameObject shieldEffectPrefab;
    [SerializeField] private float shieldDuration = 3f;
    private NetworkObject _activeShieldNetEffect;
    private double _activeShieldEndTime = 0d;
    private double _nextShieldReadyTime = 0d;
    
    // Shooting
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.5f;
    private float lastShotTime = 0f;

    // Name sync
    public string PlayerName = "";

    public TMP_Text nameText;
    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;
    public float friction = 0.85f;
    public int Team { get; set; }
    private Vector3 currentVelocity = Vector3.zero;

    public void SetName(string name)
    {
        PlayerName = name;

        if (Object != null && Object.HasInputAuthority && Runner != null)
        {
            RPC_SetName(name);
        }
    }

    public override void Render()
    {
        if (nameText != null)
        {
            nameText.text = PlayerName;
        }
    }

    public override void Spawned()
    {
        gameObject.tag = "Player";

        ConfigureLocalCameraOwnership();

        if (nameText == null)
        {
            nameText = GetComponentInChildren<TMP_Text>();
        }

        if (Object.HasStateAuthority)
        {
            Team = 0;
        }

        if (Object.HasInputAuthority)
        {
            TryAssignLocalCamera();

            if (!string.IsNullOrEmpty(PlayerName))
            {
                RPC_SetName(PlayerName);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        PlayerInputData inputData = default;
        bool hasInput = GetInput(out inputData);

        if (Object.HasInputAuthority)
        {
            TryAssignLocalCamera();
        }

        if (Object.HasStateAuthority && _activeShieldNetEffect != null)
        {
            _activeShieldNetEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);

            if (Runner != null && Runner.SimulationTime >= _activeShieldEndTime)
            {
                Runner.Despawn(_activeShieldNetEffect);
                _activeShieldNetEffect = null;
            }
        }

        if (Object.HasStateAuthority)
        {
            Vector2 input = hasInput ? new Vector2(inputData.moveX, inputData.moveY) : Vector2.zero;
            Vector3 targetVelocity = new Vector3(input.x, 0f, input.y) * moveSpeed;

            if (input.magnitude < 0.01f)
            {
                currentVelocity *= friction;
            }
            else
            {
                currentVelocity = targetVelocity;
            }

            if (currentVelocity.magnitude > 0.01f)
            {
                transform.position += currentVelocity * Runner.DeltaTime;
            }
            else
            {
                currentVelocity = Vector3.zero;
            }

            if (input.magnitude > 0.1f && lowBody != null)
            {
                Vector3 moveDir = new Vector3(input.x, 0f, input.y);
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                lowBody.rotation = Quaternion.Slerp(lowBody.rotation, targetRot, rotateSpeed * Runner.DeltaTime);
            }
        }

        if (Object.HasInputAuthority && turret != null)
        {
            RotateTurret();
        }

        if (hasInput && inputData.isShooting && Object.HasInputAuthority)
        {
            TryShoot();
        }

        if (hasInput && inputData.isShield && Object.HasStateAuthority)
        {
            if (Runner != null && Runner.SimulationTime >= _nextShieldReadyTime)
            {
                _nextShieldReadyTime = Runner.SimulationTime + shieldDuration;
                _activeShieldEndTime = Runner.SimulationTime + shieldDuration;

                if (_activeShieldNetEffect != null)
                {
                    Runner.Despawn(_activeShieldNetEffect);
                    _activeShieldNetEffect = null;
                }

                if (shieldEffectPrefab != null)
                {
                    Runner.Spawn(
                        shieldEffectPrefab,
                        transform.position,
                        transform.rotation,
                        Object.InputAuthority,
                        (runner, obj) =>
                        {
                            _activeShieldNetEffect = obj;
                        }
                    );
                }
            }
        }
    }

    void TryShoot()
    {
        if (Time.time - lastShotTime >= shootCooldown && bulletPrefab != null && firePoint != null)
        {
            RPC_Shoot();
            lastShotTime = Time.time;
        }
    }

    void RotateTurret()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                turret.rotation = Quaternion.Lerp(turret.rotation, targetRot, 100f * Time.deltaTime);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Shoot()
    {
        if (bulletPrefab == null || firePoint == null || !Object.HasStateAuthority)
        {
            return;
        }

        Vector3 spawnPos = firePoint.position + firePoint.forward * 0.5f;
        Runner.Spawn(
            bulletPrefab,
            spawnPos,
            firePoint.rotation,
            null,
            (runner, obj) =>
            {
                var bullet = obj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.Team = Team;
                    bullet.speed = 20f;
                }
            }
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SetName(string name)
    {
        PlayerName = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendChat(string message)
    {
        string sender = string.IsNullOrWhiteSpace(PlayerName) ? "Player" : PlayerName;
        Chat.ReceiveNetworkMessage($"{sender}: {message}");
    }

    void TryAssignLocalCamera()
    {
        if (Runner == null || Object == null)
        {
            return;
        }

        // Prevent remote objects from stealing this client's camera target.
        if (Object.InputAuthority != Runner.LocalPlayer)
        {
            return;
        }

        if (cam == null)
        {
            cam = GetComponentInChildren<CinemachineCamera>(true);
        }

        if (cam == null)
        {
            cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam == null)
            {
                return;
            }
        }

        if (!cam.gameObject.activeSelf)
        {
            cam.gameObject.SetActive(true);
        }

        if (cam.Follow != transform)
        {
            cam.Follow = transform;
        }

        Transform lookTarget = turret != null ? turret : transform;
        if (cam.LookAt != lookTarget)
        {
            cam.LookAt = lookTarget;
        }
    }

    void ConfigureLocalCameraOwnership()
    {
        if (Runner == null || Object == null)
        {
            return;
        }

        bool isLocalOwner = Object.HasInputAuthority && Object.InputAuthority == Runner.LocalPlayer;

        var childCameras = GetComponentsInChildren<CinemachineCamera>(true);
        foreach (var childCamera in childCameras)
        {
            if (childCamera == null)
            {
                continue;
            }

            if (childCamera.gameObject.activeSelf != isLocalOwner)
            {
                childCamera.gameObject.SetActive(isLocalOwner);
            }

            if (isLocalOwner && cam == null)
            {
                cam = childCamera;
            }
        }

        var childCameraFollow = GetComponentsInChildren<CameraFollow>(true);
        foreach (var follow in childCameraFollow)
        {
            if (follow != null)
            {
                follow.enabled = isLocalOwner;
            }
        }
    }
}