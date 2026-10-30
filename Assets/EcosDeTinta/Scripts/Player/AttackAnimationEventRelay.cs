using UnityEngine;

public class AttackAnimationEventRelay : MonoBehaviour
{
    private AttackController attackController;

    private void Awake()
    {
        attackController =
            transform.root.GetComponentInChildren<AttackController>(true);

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
}