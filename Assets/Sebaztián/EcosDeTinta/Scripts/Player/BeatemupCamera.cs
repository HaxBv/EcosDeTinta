using Unity.Cinemachine;
using UnityEngine;
public class BeatemupCamera : CinemachineExtension
{
    [Header("Level Bounds (eje X)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;

    [Header("Axis Lock")]
    [SerializeField] private bool lockY = true;
    [SerializeField] private bool lockZ = true;

    private bool initialized;
    private float lockedY;
    private float lockedZ;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Body es la etapa donde se calcula la posición de la cámara
        if (stage != CinemachineCore.Stage.Body) return;

        // Captura la altura y distancia con las que colocaste la cámara en la escena
        if (!initialized)
        {
            lockedY = transform.position.y;
            lockedZ = transform.position.z;
            initialized = true;
        }

        Vector3 pos = state.RawPosition;

        if (useBounds) pos.x = Mathf.Clamp(pos.x, minX, maxX);
        if (lockY) pos.y = lockedY;
        if (lockZ) pos.z = lockedZ;

        state.RawPosition = pos;
    }

    private void OnValidate()
    {
        if (maxX < minX) maxX = minX;
    }
}
