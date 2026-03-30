using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public override void FixedUpdateNetwork()
    {
        Vector2 Input = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(Input.x, 0, Input.y);
        characterController.Move(move * Time.deltaTime * 20f);
    }
}
