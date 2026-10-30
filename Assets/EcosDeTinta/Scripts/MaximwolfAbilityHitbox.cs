using UnityEngine;

public class MaximwolfAbilityHitbox : MonoBehaviour
{
    private AbilityController abilityController;

    private void Awake()
    {
        abilityController = GetComponentInParent<AbilityController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        abilityController?.RegisterMaximwolfHit(other);
    }
}