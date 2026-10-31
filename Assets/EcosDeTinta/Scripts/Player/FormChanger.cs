using UnityEngine;

public class FormChanger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Animator characterAnimator;

    [Header("Formas: 0 = base, 1 en adelante = transformaciones")]
    [SerializeField] private FormData[] forms = new FormData[4];

    private int currentFormIndex = -1;
    [SerializeField] private AttackController attackController;

    private void Awake()
    {
        if (attackController == null)
            attackController = GetComponentInChildren<AttackController>(true);

        ChangeForm(0);
    }

    public void ChangeForm(int formIndex)
    {
        if (formIndex < 0 || formIndex >= forms.Length)
        {
            Debug.LogWarning($"Índice de forma inválido: {formIndex}", this);
            return;
        }

        FormData selectedForm = forms[formIndex];

        if (selectedForm == null)
        {
            Debug.LogWarning($"No hay un FormData asignado en el índice {formIndex}.", this);
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

        playerStats.SetForm(selectedForm);
        characterSprite.sprite = selectedForm.FormSprite;

        if (selectedForm.Animator != null)
        {
            characterAnimator.runtimeAnimatorController = selectedForm.Animator;
        }
        else
        {
            Debug.LogWarning(
                $"La forma {selectedForm.FormName} no tiene un Animator Controller asignado en su FormData.",
                selectedForm
            );
        }

        currentFormIndex = formIndex;
        Debug.Log($"Forma cambiada a: {selectedForm.FormName} (índice {formIndex})", this);
    }

    public void ChangeFormForced(int formIndex)
    {
        // Ejecuta aquí la misma lógica interna que ya usa ChangeForm,
        // pero sin comprobar el bloqueo de cambio manual.
    }
}