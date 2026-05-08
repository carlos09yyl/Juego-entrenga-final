using UnityEngine;

/// <summary>
/// Animates the 3D world target indicator with a pulsing scale effect.
/// Attach to the TargetIndicator GameObject.
/// </summary>
public class TargetIndicatorAnimator : MonoBehaviour
{
    [Header("Pulse settings")]
    public float pulseSpeed     = 3.5f;
    public float pulseAmplitude = 0.12f;  // fraction of base scale

    [Header("Rotation")]
    public float rotationSpeed  = 90f;    // degrees per second

    private Vector3 baseScale;

    void OnEnable()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Pulsing scale
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        transform.localScale = baseScale * pulse;

        // Spin on Y axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
