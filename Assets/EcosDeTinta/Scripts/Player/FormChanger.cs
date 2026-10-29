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

    private void Awake()
    {
        ChangeForm(0);
    }
    private void Start()
    {
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

        if (playerStats == null || characterSprite == null)
        {
            Debug.LogError("Asigna PlayerStats y CharacterSprite en el Inspector.", this);
            return;
        }

        if (currentFormIndex == formIndex)
            return;

        playerStats.SetForm(selectedForm);
        characterSprite.sprite = selectedForm.FormSprite;

        if (characterAnimator != null)
            characterAnimator.runtimeAnimatorController = selectedForm.Animator;

        currentFormIndex = formIndex;

        Debug.Log($"Forma cambiada a: {selectedForm.FormName} (índice {formIndex})", this);
    }
}