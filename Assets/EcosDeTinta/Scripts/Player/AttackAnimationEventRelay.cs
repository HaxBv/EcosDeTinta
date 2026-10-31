using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class AttackAnimationEventRelay : MonoBehaviour
{
    private AttackController attackController;
    private AbilityController abilityController;

    private MovementController movement;
    private PlayerStats stats;


    public void LaunchZeroMissilesFromAnimation()
    {
        Debug.Log("[Relay] Evento de lanzamiento recibido.", this);
        abilityController?.LaunchZeroMissilesFromAnimation();
        SoundManager.Instance.PlaySFX(SoundType.SFX, 0);


    }
    private void Awake()
    {
        attackController =
            transform.root.GetComponentInChildren<AttackController>(true);

        abilityController =
            transform.root.GetComponentInChildren<AbilityController>(true);

        if (abilityController == null)
            Debug.LogError("No se encontró AbilityController bajo Player.", this);
        if (attackController == null)
            Debug.LogError("No se encontró AttackController bajo Player.", this);

        movement = transform.root.GetComponent<MovementController>();
        stats = GetComponent<PlayerStats>();
    }

    public void ActivateHitboxFromAnimation()
    {
        attackController?.ActivateHitboxFromAnimation();

    }

    // Este nombre debe coincidir con el Animation Event del clip.
    public void FinishActionAnimation()
    {
        attackController?.EndAttackAnimation();
    }

    public void LaunchShadowDaggerFromAnimation()
    {
        abilityController?.LaunchShadowDaggerFromAnimation();
    }
    public void FinishAbilityAnimation()
    {
        abilityController?.FinishAbilityAnimation();

        movement?.UnlockMovement();
    }
    public void ActivateMaximwolfHitboxFromAnimation()
    {
        abilityController?.ActivateMaximwolfHitboxFromAnimation();
    }

    public void ActivateInkmanHammerFromAnimation()
    {
        abilityController?.ActivateInkmanHammerFromAnimation();
    }
}