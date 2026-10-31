using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStats))]
public class OutOfControlManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private FormChanger formChanger;
    [SerializeField] private Button[] formButtons;

    [Header("Formas: 0 = Inkman; 1-3 = transformaciones")]
    [SerializeField] private FormData[] forms = new FormData[4];

    [Header("Control")]
    [SerializeField] private float maxControl = 100f;
    [SerializeField] private float controlLossPerSecond = 5f;
    [SerializeField] private float outOfControlDuration = 9f;

    public float CurrentControl { get; private set; }
    public bool IsOutOfControl { get; private set; }

    // Los controladores de movimiento, ataque y habilidad escucharán este evento.
    public event Action<FormData, bool> FormEffectChanged;
    public event Action<float, float> ControlChanged;
    public event Action<bool> FormChangeAvailabilityChanged;

    private float outOfControlTimer;
    private int formBeforeOutOfControl;
    private bool startedAsInkman;

    private int[] inkmanFormOrder;
    private int inkmanFormsVisited;
    private int currentInkmanOrderIndex;
    private float nextInkmanChangeTime;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();

        CurrentControl = maxControl;
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

        float multiplier = stats.CurrentForm.ControlLossMultiplier;
        float loss = controlLossPerSecond * multiplier * Time.deltaTime;

        CurrentControl = Mathf.Max(0f, CurrentControl - loss);
        ControlChanged?.Invoke(CurrentControl, maxControl);

        if (CurrentControl <= 0f)
            BeginOutOfControl();
    }

    private void BeginOutOfControl()
    {
        int currentIndex = GetCurrentFormIndex();

        if (currentIndex < 0)
        {
            Debug.LogError("La forma actual no está asignada en el arreglo forms.", this);
            return;
        }

        IsOutOfControl = true;
        formBeforeOutOfControl = currentIndex;
        startedAsInkman = currentIndex == 0;
        outOfControlTimer = outOfControlDuration;

        SetFormButtonsInteractable(false);
        FormChangeAvailabilityChanged?.Invoke(false);

        if (startedAsInkman)
        {
            PrepareInkmanFormOrder();
            inkmanFormsVisited = 0;
            currentInkmanOrderIndex = -1;

            // Entra inmediatamente en una transformación.
            ChangeInkmanForm();
            nextInkmanChangeTime = outOfControlDuration / 3f;
        }
        else
        {
            FormEffectChanged?.Invoke(stats.CurrentForm, true);
        }

        Debug.Log(
            $"Pérdida de control iniciada en {stats.CurrentForm.FormName}.",
            this
        );
    }

    private void UpdateOutOfControl()
    {
        outOfControlTimer -= Time.deltaTime;

        if (startedAsInkman)
        {
            float elapsed = outOfControlDuration - outOfControlTimer;

            if (inkmanFormsVisited < 3 && elapsed >= nextInkmanChangeTime)
            {
                ChangeInkmanForm();
                nextInkmanChangeTime = outOfControlDuration
                    * (inkmanFormsVisited + 1) / 3f;
            }
        }

        if (outOfControlTimer <= 0f)
            EndOutOfControl();
    }

    private void ChangeInkmanForm()
    {
        if (currentInkmanOrderIndex >= 0)
            FormEffectChanged?.Invoke(stats.CurrentForm, false);

        currentInkmanOrderIndex++;
        inkmanFormsVisited++;

        int nextFormIndex = inkmanFormOrder[currentInkmanOrderIndex];
        formChanger.ChangeFormForced(nextFormIndex);

        FormEffectChanged?.Invoke(stats.CurrentForm, true);

        Debug.Log(
            $"Inkman fuera de control cambió a {stats.CurrentForm.FormName}.",
            this
        );
    }

    private void EndOutOfControl()
    {
        if (stats.CurrentForm != null)
            FormEffectChanged?.Invoke(stats.CurrentForm, false);

        if (startedAsInkman)
            formChanger.ChangeFormForced(0);
        else
            formChanger.ChangeFormForced(formBeforeOutOfControl);

        IsOutOfControl = false;
        CurrentControl = maxControl;

        SetFormButtonsInteractable(true);
        FormChangeAvailabilityChanged?.Invoke(true);
        ControlChanged?.Invoke(CurrentControl, maxControl);

        Debug.Log("Terminó la pérdida de control.", this);
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

    private void PrepareInkmanFormOrder()
    {
        inkmanFormOrder = new[] { 1, 2, 3 };

        // Baraja el orden, pero garantiza que Inkman pase por las tres formas.
        for (int i = inkmanFormOrder.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (inkmanFormOrder[i], inkmanFormOrder[randomIndex]) =
                (inkmanFormOrder[randomIndex], inkmanFormOrder[i]);
        }
    }

    private void SetFormButtonsInteractable(bool value)
    {
        if (formButtons == null)
            return;

        foreach (Button button in formButtons)
        {
            if (button != null)
                button.interactable = value;
        }
    }
}