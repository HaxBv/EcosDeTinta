using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInputs))]
public class MovementController : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private CharacterController controller;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpCooldown = 0.15f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    public event Action<float> OnSpeedChanged;
    public event Action<bool> OnSprintChanged;
    public event Action OnJumped;
    public event Action OnLanded;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float jumpCooldownTimer;

    private bool isSprinting;
    private bool wasGrounded;
    private bool canJump = true;


    [SerializeField] private Transform visual;

    private Vector3 visualOriginalScale;

    public int FacingDirection { get; private set; } = 1;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        playerInputs = GetComponent<PlayerInputs>();
        if (visual != null)
            visualOriginalScale = visual.localScale;
    }

    private void OnEnable()
    {
        playerInputs.MoveChanged += OnMove;
        playerInputs.JumpPressed += OnJump;
        playerInputs.SprintChanged += OnSprint;
    }

    private void OnDisable()
    {
        playerInputs.MoveChanged -= OnMove;
        playerInputs.JumpPressed -= OnJump;
        playerInputs.SprintChanged -= OnSprint;
    }

    private void Update()
    {
        if (stats.CurrentForm == null)
            return;
        if (moveInput.x != 0f)
        {
            FacingDirection = moveInput.x > 0f ? 1 : -1;

            if (visual != null)
            {
                visual.localScale = new Vector3(
                    Mathf.Abs(visualOriginalScale.x) * FacingDirection,
                    visualOriginalScale.y,
                    visualOriginalScale.z
                );
            }
        }

        ApplyMovement(Time.deltaTime);
        ApplyMovement(Time.deltaTime);
        UpdateJumpAvailability(Time.deltaTime);
    }

    private void OnMove(Vector2 value)
    {
        moveInput = value;
    }

    private void OnJump()
    {
        if (!canJump || !controller.isGrounded)
            return;

        canJump = false;
        jumpCooldownTimer = jumpCooldown;

        verticalVelocity = Mathf.Sqrt(stats.JumpHeight * -2f * gravity);
        OnJumped?.Invoke();
    }

    private void OnSprint(bool sprint)
    {
        if (sprint == isSprinting)
            return;

        isSprinting = sprint;
        OnSprintChanged?.Invoke(isSprinting);
    }

    private void ApplyMovement(float deltaTime)
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        float targetSpeed = isSprinting
            ? stats.SprintSpeed
            : stats.WalkSpeed;

        Vector3 desiredVelocity =
            Vector3.ClampMagnitude(direction, 1f) * targetSpeed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            desiredVelocity,
            stats.Acceleration * deltaTime
        );

        if (wasGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * deltaTime;

        controller.Move(
            (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime
        );

        bool grounded = controller.isGrounded;

        if (grounded && !wasGrounded)
            OnLanded?.Invoke();

        wasGrounded = grounded;
        OnSpeedChanged?.Invoke(horizontalVelocity.magnitude);
    }

    private void UpdateJumpAvailability(float deltaTime)
    {
        if (canJump)
            return;

        jumpCooldownTimer -= deltaTime;

        if (jumpCooldownTimer <= 0f && controller.isGrounded)
            canJump = true;
    }
}