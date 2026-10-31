using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInputs))]
public class MovementController : MonoBehaviour
{
    private static readonly int RunningHash = Animator.StringToHash("Running");
    private OutOfControlManager outOfControlManager;
    private bool maximwolfOutOfControl;
    private bool zeroOutOfControl;

    [Header("Dash fuera de control de Zero")]
    [SerializeField] private int zeroDashDirection = 1;
    [SerializeField] private float zeroDashInterval = 2f;
    [SerializeField] private float zeroDashDuration = 0.2f;
    [SerializeField] private float zeroDashSpeed = 12f;
    private bool wasZeroOutOfControl;
    private bool isZeroDashing;
    private float zeroDashCooldown;
    private float zeroDashRemaining;
    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private CharacterController controller;
    private Animator characterAnimator;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpCooldown = 0.15f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Visual")]
    [SerializeField] private Transform visual;

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

    private Vector3 visualOriginalScale;

    public int FacingDirection { get; private set; } = 1;
    private bool sprintHeld;
    private bool sprintExhausted;
    public bool IsMovementLocked { get; private set; }
    private bool ShouldStopHorizontalMovement =>
    IsMovementLocked && controller.isGrounded;

    public void LockMovement()
    {
        IsMovementLocked = true;

        isSprinting = false;
        OnSprintChanged?.Invoke(false);

        if (controller.isGrounded)
            horizontalVelocity = Vector3.zero;
    }

    public void UnlockMovement()
    {
        IsMovementLocked = false;
    }
    private void Awake()
    {
        outOfControlManager = GetComponent<OutOfControlManager>();
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        playerInputs = GetComponent<PlayerInputs>();

        // Encuentra el Animator del hijo Sprite.
        characterAnimator = GetComponentInChildren<Animator>(true);

        if (visual != null)
            visualOriginalScale = visual.localScale;

        if (characterAnimator == null)
            Debug.LogError("No se encontró un Animator en Player o sus hijos.", this);
    }

    private void OnEnable()
    {
        playerInputs.MoveChanged += OnMove;
        playerInputs.JumpPressed += OnJump;
        playerInputs.SprintChanged += OnSprint;

        if (outOfControlManager != null)
            outOfControlManager.FormEffectChanged += OnOutOfControlFormChanged;
    }

    private void OnDisable()
    {
        playerInputs.MoveChanged -= OnMove;
        playerInputs.JumpPressed -= OnJump;
        playerInputs.SprintChanged -= OnSprint;
        if (outOfControlManager != null)
            outOfControlManager.FormEffectChanged -= OnOutOfControlFormChanged;

        if (characterAnimator != null)
            characterAnimator.SetBool(RunningHash, false);
    }
    private void OnOutOfControlFormChanged(FormData form, bool isActive)
    {
        if (form == null)
            return;

        if (form.FormName == "Maximwolf")
            maximwolfOutOfControl = isActive;

        if (form.FormName == "Zero")
        {
            zeroOutOfControl = isActive;
            zeroDashCooldown = zeroDashInterval;
            zeroDashRemaining = 0f;
        }
    }
    private void Update()
    {
        if (stats == null || stats.CurrentForm == null)
            return;

        float deltaTime = Time.deltaTime;
        UpdateZeroDash(deltaTime);
        UpdateFacingDirection();
        UpdateSprintAndStamina(deltaTime);
        ApplyMovement(deltaTime);
        UpdateJumpAvailability(deltaTime);
        UpdateRunningAnimation();
    }
    private void UpdateZeroDash(float deltaTime)
    {
        string formName = stats.CurrentForm != null
            ? stats.CurrentForm.FormName
            : string.Empty;

        bool isZero =
            formName.Equals("Zero", StringComparison.OrdinalIgnoreCase) ||
            formName.Equals("Zeroes", StringComparison.OrdinalIgnoreCase);

        bool zeroIsOutOfControl =
            outOfControlManager != null &&
            outOfControlManager.IsOutOfControl &&
            isZero;

        if (zeroIsOutOfControl != wasZeroOutOfControl)
        {
            wasZeroOutOfControl = zeroIsOutOfControl;
            zeroDashCooldown = zeroDashInterval;

            if (!zeroIsOutOfControl)
            {
                isZeroDashing = false;
                zeroDashRemaining = 0f;
            }

            Debug.Log($"Dash de Zero activo: {zeroIsOutOfControl}", this);
        }

        if (!zeroIsOutOfControl || isZeroDashing)
            return;

        zeroDashCooldown -= deltaTime;

        if (zeroDashCooldown <= 0f)
        {
            zeroDashDirection = FacingDirection;
            zeroDashRemaining = zeroDashDuration;
            isZeroDashing = true;
            zeroDashCooldown = zeroDashInterval;

            Debug.Log($"¡Zero inició el dash hacia {zeroDashDirection}!", this);
        }
    }
    private void UpdateFacingDirection()
    {
        if (ShouldStopHorizontalMovement || moveInput.x == 0f)
            return;
        Vector2 facingInput = maximwolfOutOfControl ? -moveInput : moveInput;

        if (ShouldStopHorizontalMovement || facingInput.x == 0f)
            return;

        FacingDirection = facingInput.x > 0f ? 1 : -1;

        if (visual != null)
        {
            visual.localScale = new Vector3(
                Mathf.Abs(visualOriginalScale.x) * FacingDirection,
                visualOriginalScale.y,
                visualOriginalScale.z
            );
        }

    }

    private void UpdateRunningAnimation()
    {
        if (characterAnimator == null)
            return;

        bool isRunning = isZeroDashing ||
            (isSprinting && moveInput.sqrMagnitude > 0.01f);

        characterAnimator.SetBool(RunningHash, isRunning);
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
        sprintHeld = sprint;

        if (!sprint)
            SetSprinting(false);
    }

    private void UpdateSprintAndStamina(float deltaTime)
    {
        if (ShouldStopHorizontalMovement)
        {
            SetSprinting(false);
            return;
        }

        if (sprintExhausted)
        {
            SetSprinting(false);
            stats.RecoverStamina(stats.StaminaRecoveryPerSecond * deltaTime);

            // Tras agotarse, se desbloquea al llenar la barra y soltar Shift.
            if (!sprintHeld && stats.CurrentStamina >= stats.MaxStamina)
                sprintExhausted = false;

            return;
        }

        bool isRunning =
    !ShouldStopHorizontalMovement &&
    isSprinting &&
    moveInput.sqrMagnitude > 0.01f;

        characterAnimator.SetBool(RunningHash, isRunning);
        bool hasMovementInput = moveInput.sqrMagnitude > 0.01f;
        bool canSprint =
            sprintHeld &&
            hasMovementInput &&
            stats.CurrentStamina > 0f;

        if (canSprint)
        {
            SetSprinting(true);
            stats.ConsumeStamina(stats.SprintStaminaCostPerSecond * deltaTime);

            if (stats.CurrentStamina <= 0f)
            {
                sprintExhausted = true;
                SetSprinting(false);
            }
        }
        else
        {
            SetSprinting(false);
            stats.RecoverStamina(stats.StaminaRecoveryPerSecond * deltaTime);
        }
    }

    private void SetSprinting(bool value)
    {
        if (isSprinting == value)
            return;

        isSprinting = value;
        OnSprintChanged?.Invoke(isSprinting);
    }
    private void ApplyMovement(float deltaTime)
    {
        // Determina el movimiento horizontal.
        if (isZeroDashing && zeroDashRemaining > 0f)
        {
            horizontalVelocity = Vector3.right * zeroDashDirection * zeroDashSpeed;
        }
        else if (ShouldStopHorizontalMovement)
        {
            horizontalVelocity = Vector3.zero;
        }
        else
        {
            Vector2 movementInput =
                maximwolfOutOfControl ? -moveInput : moveInput;

            Vector3 direction =
                new Vector3(movementInput.x, 0f, movementInput.y);

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
        }

        // El salto y la gravedad siguen activos durante el dash.
        if (wasGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;
        else
            verticalVelocity += gravity * deltaTime;

        controller.Move(
            (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime
        );

        // Descuenta el tiempo del dash después de aplicar su movimiento.
        if (isZeroDashing)
        {
            zeroDashRemaining -= deltaTime;

            if (zeroDashRemaining <= 0f)
            {
                zeroDashRemaining = 0f;
                isZeroDashing = false;
                horizontalVelocity = Vector3.zero;
            }
        }

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