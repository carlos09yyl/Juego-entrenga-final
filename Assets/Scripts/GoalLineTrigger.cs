using UnityEngine;

public class GoalLineTrigger : MonoBehaviour
{
    [Header("References")]
    public PenaltyResetController resetController;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Ball")) return;

        triggered = true;
        Debug.Log("⚽ GOL DETECTADO");

        ScoreData.WhiteScore++;

        // Usamos el reset suave para no recargar la escena y perder datos
        Invoke(nameof(ExecuteSoftReset), 2f);
    }

    private void ExecuteSoftReset()
    {
        if (resetController != null)
        {
            resetController.FullReset();
            triggered = false;
        }
    }
}