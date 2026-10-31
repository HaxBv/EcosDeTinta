using UnityEngine;

public class FormChanger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private AttackController attackController;
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private OutOfControlManager outOfControlManager;

    [Header("Formas: 0 = base, 1 en adelante = transformaciones")]
    [SerializeField] private FormData[] forms = new FormData[4];

    private int currentFormIndex = -1;

    public int CurrentFormIndex => currentFormIndex;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (attackController == null)
            attackController = GetComponentInChildren<AttackController>(true);

        if (outOfControlManager == null)
            outOfControlManager = GetComponent<OutOfControlManager>();

        if (abilityController == null)
            abilityController = GetComponent<AbilityController>();

        ApplyForm(0);
    }

    // Este método se usa desde los botones. No permite cambios manuales
    // mientras el OutOfControlManager tenga activo el episodio.
    public void ChangeForm(int formIndex)
    {
        if (outOfControlManager != null && outOfControlManager.IsOutOfControl)
        {
            Debug.Log("No puedes cambiar de forma durante la pérdida de control.", this);
            return;
        }

        ApplyForm(formIndex);
    }

    // Lo llama OutOfControlManager para la rotación automática de Inkman.
    public void ChangeFormForced(int formIndex)
    {
        ApplyForm(formIndex);
    }

    private void ApplyForm(int formIndex)
    {
        if (formIndex < 0 || formIndex >= forms.Length)
        {
            Debug.LogWarning($"Índice de forma inválido: {formIndex}", this);
            return;
        }

        FormData selectedForm = forms[formIndex];

        if (selectedForm == null)
        {
            Debug.LogWarning(
                $"No hay un FormData asignado en el índice {formIndex}.",
                this
            );
            return;
        }

        if (playerStats == null || characterSprite == null || characterAnimator == null)
        {
            Debug.LogError(
                "Asigna PlayerStats, CharacterSprite y CharacterAnimator en el Inspector.",
                this
            );
            return;
        }

        if (currentFormIndex == formIndex)
            return;

        attackController?.CancelAttack();
        abilityController?.CancelAbilityForFormChange();

        // Después de la limpieza, cambia el Animator Controller.
        characterAnimator.runtimeAnimatorController = selectedForm.Animator;

        playerStats.SetForm(selectedForm);
        characterSprite.sprite = selectedForm.FormSprite;

        if (selectedForm.Animator != null)
        {
            characterAnimator.runtimeAnimatorController = selectedForm.Animator;
        }
        else
        {
            Debug.LogWarning(
                $"La forma {selectedForm.FormName} no tiene Animator Controller asignado.",
                selectedForm
            );
        }

        currentFormIndex = formIndex;

        Debug.Log(
            $"Forma cambiada a: {selectedForm.FormName} (índice {formIndex})",
            this
        );
    }
}