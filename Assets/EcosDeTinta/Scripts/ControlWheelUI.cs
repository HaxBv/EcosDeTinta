using UnityEngine;
using UnityEngine.UI;

public class ControlWheelUI : MonoBehaviour
{
    [SerializeField] private OutOfControlManager controlManager;
    [SerializeField] private Image fillImage;

    [Header("Colores")]
    [SerializeField] private Color controlColor = Color.black;
    [SerializeField] private Color outOfControlColor = Color.red;

    private void Awake()
    {
        if (fillImage == null)
            return;

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
    }

    private void Update()
    {
        if (controlManager == null || fillImage == null)
            return;

        if (controlManager.IsOutOfControl)
        {
            fillImage.color = outOfControlColor;

            float duration = Mathf.Max(
                0.01f,
                controlManager.OutOfControlDuration
            );

            float elapsed =
                duration - controlManager.OutOfControlTimeRemaining;

            fillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
        }
        else
        {
            fillImage.color = controlColor;

            float maxControl = Mathf.Max(0.01f, controlManager.MaxControl);

            fillImage.fillAmount = Mathf.Clamp01(
                controlManager.CurrentControl / maxControl
            );
        }
    }
}