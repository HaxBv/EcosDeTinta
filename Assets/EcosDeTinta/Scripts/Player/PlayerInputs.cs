using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    private static readonly int AbilityHash = Animator.StringToHash("Ability");

    private InputSystem_Actions inputs;

    [Header("Animación")]
    [SerializeField] private Animator characterAnimator;

    public event Action<Vector2> MoveChanged;
    public event Action JumpPressed;
    public event Action<bool> SprintChanged;
    public event Action AttackPressed;
    public event Action AbilityPressed;

    private void Awake()
    {
        inputs = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputs.Player.Move.performed += OnMove;
        inputs.Player.Move.canceled += OnMove;
        inputs.Player.Jump.performed += OnJump;
        inputs.Player.Sprint.performed += OnSprint;
        inputs.Player.Sprint.canceled += OnSprint;
        inputs.Player.Attack.performed += OnAttack;
        inputs.Player.Ability.performed += OnAbility;

        inputs.Player.Enable();
    }

    private void OnDisable()
    {
        inputs.Player.Move.performed -= OnMove;
        inputs.Player.Move.canceled -= OnMove;
        inputs.Player.Jump.performed -= OnJump;
        inputs.Player.Sprint.performed -= OnSprint;
        inputs.Player.Sprint.canceled -= OnSprint;
        inputs.Player.Attack.performed -= OnAttack;
        inputs.Player.Ability.performed -= OnAbility;

        inputs.Player.Disable();
    }

    private void OnDestroy()
    {
        inputs?.Dispose();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveChanged?.Invoke(context.ReadValue<Vector2>());
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            JumpPressed?.Invoke();
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        SprintChanged?.Invoke(context.performed);
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Debug.Log("[PlayerInputs] Se pulsó Attack.");
        AttackPressed?.Invoke();
    }

    private void OnAbility(InputAction.CallbackContext context)
    {
        if (context.performed)
            AbilityPressed?.Invoke();
    }
}