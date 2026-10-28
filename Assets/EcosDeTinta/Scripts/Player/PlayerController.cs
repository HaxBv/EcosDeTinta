using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions inputs;
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float rotationSpeed = 720f;   

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpCooldown = 0.15f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    // Actions 
    public event Action<float> OnSpeedChanged;   
    public event Action<bool> OnSprintChanged;
    public event Action OnJumped;
    public event Action OnLanded;

    
    private CharacterController controller;

    
    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool isSprinting;
    private bool wasGrounded;
    private bool canJump = true;

    //Coroutines
    private Coroutine movementCoroutine;
    private Coroutine jumpCoroutine;
    private WaitForSeconds waitJumpCooldown;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputs = new InputSystem_Actions();
        waitJumpCooldown = new WaitForSeconds(jumpCooldown);
    }

    private void OnEnable()
    {
        inputs.Player.Enable();

        inputs.Player.Move.performed += OnMove;
        inputs.Player.Move.canceled += OnMove;
        inputs.Player.Jump.performed += OnJump;
        inputs.Player.Sprint.performed += OnSprint;
        inputs.Player.Sprint.canceled += OnSprint;

        canJump = true;
        StartMovement(); 
    }

    private void OnDisable()
    {
        inputs.Player.Move.performed -= OnMove;
        inputs.Player.Move.canceled -= OnMove;
        inputs.Player.Jump.performed -= OnJump;
        inputs.Player.Sprint.performed -= OnSprint;
        inputs.Player.Sprint.canceled -= OnSprint;

        inputs.Player.Disable();

        StopAllCoroutines();
        movementCoroutine = null;
        jumpCoroutine = null;
    }

    private void OnDestroy() => inputs?.Dispose();

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        if (moveInput != Vector2.zero) StartMovement();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!canJump || !controller.isGrounded) return;
        jumpCoroutine = StartCoroutine(JumpRoutine());
    }

    private void OnSprint(InputAction.CallbackContext ctx)
    {
        bool sprint = ctx.performed;
        if (sprint == isSprinting) return;

        isSprinting = sprint;
        OnSprintChanged?.Invoke(isSprinting);
        StartMovement();
    }

 
    // Coroutines
    private void StartMovement()
    {
        if (!isActiveAndEnabled || movementCoroutine != null) return;
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    public IEnumerator MovementRoutine()
    {
        while (!IsAtRest())
        {
            ApplyMovement();
            yield return null;
        }

        movementCoroutine = null;
        yield break;
    }

    public IEnumerator JumpRoutine()
    {
        canJump = false;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        OnJumped?.Invoke();
        StartMovement();

        yield return waitJumpCooldown;                           // avoid double jump at first frame
        yield return new WaitUntil(() => controller.isGrounded); // wait  until landing

        canJump = true;
        jumpCoroutine = null;
        yield break;
    }

   
    // Movement Logic

    private bool IsAtRest()
    {
        return moveInput == Vector2.zero
            && horizontalVelocity.sqrMagnitude < 0.0001f
            && controller.isGrounded
            && verticalVelocity <= 0f;
    }

    private void ApplyMovement()
    {
        float dt = Time.deltaTime;

        //Horizontal
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 desiredVelocity = Vector3.ClampMagnitude(direction, 1f) * targetSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, acceleration * dt);

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * dt);
        }

        // Vertical
        if (wasGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * dt;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

        // ground state
        bool grounded = controller.isGrounded;
        if (grounded && !wasGrounded) OnLanded?.Invoke();
        wasGrounded = grounded;

        OnSpeedChanged?.Invoke(horizontalVelocity.magnitude);
    }
}
