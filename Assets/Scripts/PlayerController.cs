using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate()
    {
        Vector2 Input = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(Input.x, 0, Input.y);
        characterController.Move(move * Time.deltaTime * 5f);
    }
}
