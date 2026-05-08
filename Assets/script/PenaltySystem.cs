using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;   // 👈 IMPORTANTE

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canKick)
        {
            if (!ballController)
            {
                Debug.LogError("❌ BallController NO asignado en PenaltySystem");
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
        Debug.Log("⚽ Animación de patear se inicia");

        yield return new WaitForSeconds(kickDelay);

        if (!ballController)
        {
            Debug.LogError("❌ BallController es NULL en KickBall()");
            yield break;
        }

        Debug.Log("🚀 Balón lanzado");
        ballController.Launch(direction);
    }

    // 🔔 Llamado por GoalAreaTrigger
    public void OnBallEnteredGoalArea()
    {
        if (!goalkeeperController)
        {
            Debug.LogError("❌ GoalkeeperController NO asignado");
            return;
        }

        if (!hasGoalkeeperMoved)
        {
            hasGoalkeeperMoved = true;
            MoveGoalkeeperRandom();
        }
    }

    // 🔔 Llamado por GoalLineTrigger
    public void OnGoalScored()
    {
        Debug.Log("🥅 ¡GOL!");
        ResetPenalty();
    }

    // 🔔 Llamado cuando el arquero tapa el balón
    public void OnBallSavedByGoalkeeper()
    {
        Debug.Log("🧤 ¡El arquero atajó!");
        ResetPenalty();
    }

    void MoveGoalkeeperRandom()
    {
        if (divePoints == null || divePoints.Length == 0)
        {
            Debug.LogError("❌ DivePoints no asignados");
            return;
        }

        int index = Random.Range(0, divePoints.Length);
        Transform target = divePoints[index];

        if (!target)
        {
            Debug.LogError("❌ DivePoint NULL");
            return;
        }

        goalkeeperController.PrepareReaction(target);
        Debug.Log("🧍‍♂️ Arquero se prepara para: " + target.name);
    }

    // 🔥 RESET CORRECTO: REINICIA TODA LA ESCENA
    void ResetPenalty()
    {
        Debug.Log("🔄 Reiniciando escena completa...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}