using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// DEPRECATED — replaced entirely by PenaltyTargetSelector.
/// This component is kept for reference only and must remain disabled in the Inspector.
/// Enabling it will conflict with PenaltyTargetSelector: Space input, ball launch
/// direction and scene loading will all be handled twice.
/// </summary>
[DisallowMultipleComponent]
public class PenaltySystem : MonoBehaviour
{
    [Header("Referencias")]
    public BallController ballController;
    public GoalkeeperController goalkeeperController;
    public Transform[] divePoints;

    [Header("Tiros")]
    public float kickDelay = 2f;

    private bool hasGoalkeeperMoved = false;
    private bool canKick = true;

    void Awake()
    {
        // Self-disable to guarantee it never interferes with PenaltyTargetSelector.
        enabled = false;
        Debug.LogWarning("[PenaltySystem] Script deshabilitado — usa PenaltyTargetSelector.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canKick)
        {
            if (!ballController)
            {
                Debug.LogError("❌ BallController NO asignado");
                return;
            }

            canKick = false;

            Vector3 ballDirection = new Vector3(
                Random.Range(-1f, 1f),
                0.1f,
                1f
            ).normalized;

            StartCoroutine(KickBall(ballDirection));
        }
    }

    IEnumerator KickBall(Vector3 direction)
    {
        Debug.Log("⚽ Animación de patear");

        yield return new WaitForSeconds(kickDelay);

        if (!ballController)
        {
            Debug.LogError("❌ BallController NULL");
            yield break;
        }

        ballController.Launch(direction);
    }

    public void OnBallEnteredGoalArea()
    {
        if (!goalkeeperController) return;

        if (!hasGoalkeeperMoved)
        {
            hasGoalkeeperMoved = true;
            MoveGoalkeeperRandom();
        }
    }

    public void OnGoalScored()
    {
        Debug.Log("🥅 ¡GOL!");
        SceneManager.LoadScene("ScoreScene");
    }

    public void OnBallSavedByGoalkeeper()
    {
        Debug.Log("🧤 Atajó");
        ResetPenalty();
    }

    void MoveGoalkeeperRandom()
    {
        if (divePoints == null || divePoints.Length == 0) return;

        int index = Random.Range(0, divePoints.Length);
        Transform target = divePoints[index];

        if (!target) return;

        goalkeeperController.PrepareReaction(target);
    }

    void ResetPenalty()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}