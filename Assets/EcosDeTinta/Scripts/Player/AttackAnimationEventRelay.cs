using UnityEngine;

public class AttackAnimationEventRelay : MonoBehaviour
{
    private AttackController attackController;
    private AbilityController abilityController;

    

    public void LaunchZeroMissilesFromAnimation()
    {
        Debug.Log("[Relay] Evento de lanzamiento recibido.", this);
        abilityController?.LaunchZeroMissilesFromAnimation();
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