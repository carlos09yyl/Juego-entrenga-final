using UnityEngine;

public class GoalAreaTrigger : MonoBehaviour
{
    public PenaltySystem penaltySystem;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            Debug.Log("Balón entró en GoalArea");
            penaltySystem.OnBallEnteredGoalArea();
        }
    }
}