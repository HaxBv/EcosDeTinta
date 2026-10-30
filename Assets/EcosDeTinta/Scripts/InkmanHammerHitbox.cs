using UnityEngine;

public class InkmanHammerHitbox : MonoBehaviour
{
    private AbilityController abilityController;

    private void Awake()
    {
        abilityController = GetComponentInParent<AbilityController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        abilityController?.RegisterInkmanHammerHit(other);
    }
}