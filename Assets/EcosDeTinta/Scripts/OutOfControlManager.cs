using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStats))]
public class OutOfControlManager : MonoBehaviour
{
    private static readonly int AbilityHash =
    Animator.StringToHash("Ability");
    [SerializeField] private MovementController movement;
    [SerializeField] private Animator characterAnimator;
    [Header("Intervalo de habilidad de Shadow")]
    [SerializeField] private float shadowMinInterval = 2f;
    [SerializeField] private float shadowMaxInterval = 4f;

    private float nextShadowAbilityTime = -1f;
    [Header("Referencias")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private FormChanger formChanger;
    [SerializeField] private Button[] formButtons;

    [Header("Formas: 0 = Inkman, 1 = Maximwolf, 2 = Shadow, 3 = Zero")]
    [SerializeField] private FormData[] forms = new FormData[4];
    [Header("Intervalo de cambio de Inkman")]
    [SerializeField, Min(0.1f)] private float inkmanMinChangeInterval = 3f;
    [SerializeField, Min(0.1f)] private float inkmanMaxChangeInterval = 4f;

    private float nextInkmanChangeTime;
    [Header("Control")]
    [SerializeField, Min(1f)] private float maxControl = 100f;
    [SerializeField, Min(0f)] private float controlLossPerSecond = 5f;
    [SerializeField, Min(0.1f)] private float outOfControlDuration = 15f;

    [ShowInInspector]
    public float CurrentControl { get; private set; }
    public bool IsOutOfControl { get; private set; }

    public event Action<FormData, bool> FormEffectChanged;
    public event Action<float, float> ControlChanged;

    private float timer;
    private int startingFormIndex;
    private bool startedAsInkman;

    private int[] inkmanFormOrder;
    private int currentInkmanOrderIndex = -1;

    public float MaxControl => maxControl;
    public float OutOfControlDuration => outOfControlDuration;
    public float OutOfControlTimeRemaining =>
        IsOutOfControl ? Mathf.Max(0f, timer) : 0f;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();

        if (formChanger == null)
            formChanger = GetComponent<FormChanger>();

        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>(true);
        CurrentControl = maxControl;
        movement = GetComponent<MovementController>();

    }
    private void UpdateShadowAbility()
    {
        if (!IsOutOfControl ||
            stats.CurrentForm == null ||
            stats.CurrentForm.FormName != "Shadow" ||
            Time.time < nextShadowAbilityTime)
        {
            return;
        }

        if (characterAnimator == null)
        {
            Debug.LogError("OutOfControlManager no encontró el Animator.", this);
            return;
        }

        characterAnimator.ResetTrigger(AbilityHash);
        characterAnimator.SetTrigger(AbilityHash);
        movement.LockMovement();
        nextShadowAbilityTime = Time.time + GetRandomShadowInterval();
    }

    private float GetRandomShadowInterval()
    {
        float min = Mathf.Min(shadowMinInterval, shadowMaxInterval);
        float max = Mathf.Max(shadowMinInterval, shadowMaxInterval);

        return UnityEngine.Random.Range(min, max);
    }

    private void NotifyFormEffectChanged(FormData form, bool isActive)
    {
        if (form != null && form.FormName == "Shadow")
        {
            nextShadowAbilityTime = isActive
                ? Time.time + GetRandomShadowInterval()
                : -1f;
        }

        FormEffectChanged?.Invoke(form, isActive);
    }
    private void Update()
    {
        if (stats == null || stats.CurrentForm == null)
            return;

        if (IsOutOfControl)
        {
            UpdateOutOfControl();
            return;
        }

        float loss = controlLossPerSecond
            * stats.CurrentForm.ControlLossMultiplier
            * Time.deltaTime;

        CurrentControl = Mathf.Max(0f, CurrentControl - loss);
        ControlChanged?.Invoke(CurrentControl, maxControl);

        if (CurrentControl <= 0f)
            BeginOutOfControl();
    }

    private void BeginOutOfControl()
    {
        startingFormIndex = GetCurrentFormIndex();

        if (startingFormIndex < 0)
        {
            Debug.LogError(
                "La forma actual no está asignada en el arreglo Forms del OutOfControlManager.",
                this
            );
            return;
        }

        IsOutOfControl = true;
        timer = outOfControlDuration;
        startedAsInkman = startingFormIndex == 0;
        SetFormButtons(false);

        if (startedAsInkman)
        {
            ChangeToNextInkmanForm();
            nextInkmanChangeTime = Time.time + GetRandomInkmanInterval();
        }
        else
        {
            NotifyFormEffectChanged(stats.CurrentForm, true);
        }

        Debug.Log($"Pérdida de control: {stats.CurrentForm.FormName}", this);
    }
    private float GetRandomInkmanInterval()
    {
        float min = Mathf.Min(inkmanMinChangeInterval, inkmanMaxChangeInterval);
        float max = Mathf.Max(inkmanMinChangeInterval, inkmanMaxChangeInterval);

        return UnityEngine.Random.Range(min, max);
    }
    private void UpdateOutOfControl()
    {
        timer -= Time.deltaTime;

        if (startedAsInkman &&
            timer > 0f &&
            Time.time >= nextInkmanChangeTime)
        {
            ChangeToNextInkmanForm();
            nextInkmanChangeTime = Time.time + GetRandomInkmanInterval();
        }

        UpdateShadowAbility();

        if (timer <= 0f)
            EndOutOfControl();
    }
    private void ChangeToNextInkmanForm()
    {
        NotifyFormEffectChanged(stats.CurrentForm, false);

        int currentIndex = GetCurrentFormIndex();
        int nextIndex;

        do
        {
            nextIndex = UnityEngine.Random.Range(1, forms.Length);
        }
        while (nextIndex == currentIndex);

        formChanger.ChangeFormForced(nextIndex);
        NotifyFormEffectChanged(stats.CurrentForm, true);

        Debug.Log(
            $"Inkman cambió automáticamente a {stats.CurrentForm.FormName}.",
            this
        );
    }

    private void EndOutOfControl()
    {
        if (stats.CurrentForm != null)
            NotifyFormEffectChanged(stats.CurrentForm, false);

        formChanger.ChangeFormForced(startingFormIndex);

        IsOutOfControl = false;
        CurrentControl = maxControl;

        SetFormButtons(true);
        ControlChanged?.Invoke(CurrentControl, maxControl);

        Debug.Log("Terminó la pérdida de control.", this);
    }

    private void PrepareInkmanFormOrder()
    {
        inkmanFormOrder = new[] { 1, 2, 3 };

        for (int i = inkmanFormOrder.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (inkmanFormOrder[i], inkmanFormOrder[randomIndex]) =
                (inkmanFormOrder[randomIndex], inkmanFormOrder[i]);
        }
    }

    private int GetCurrentFormIndex()
    {
        for (int i = 0; i < forms.Length; i++)
        {
            if (forms[i] == stats.CurrentForm)
                return i;
        }

        return -1;
    }

    private void SetFormButtons(bool interactable)
    {
        if (formButtons == null)
            return;

        foreach (Button button in formButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}