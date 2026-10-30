using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    public void ActivateMeleeHitboxFromAnimation()
    {
        enemy?.ActivateMeleeHitboxFromAnimation();
    }

    public void DeactivateMeleeHitboxFromAnimation()
    {
        enemy?.DeactivateMeleeHitboxFromAnimation();
    }

    public void RangedAttackFromAnimation()
    {
        enemy?.RangedAttackFromAnimation();
    }

    public void FinishAttackAnimation()
    {
        enemy?.FinishAttackAnimation();
    }
}