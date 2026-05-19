using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central controller for the 3-attempt penalty system.
///
/// Attempt progression (0-based index):
///   0 → ball slow  / keeper slow  / low accuracy
///   1 → ball fast  / keeper medium/ medium accuracy
///   2 → ball fast+ / keeper fast  / equal speed, coin-flip accuracy
///
/// Enter       → open / close target selection
/// WASD/Arrows → navigate 3×3 grid
/// Space/Click → confirm shot
/// </summary>
public class PenaltyTargetSelector : MonoBehaviour
{
    // ── Grid constants ────────────────────────────────────────────────
    private const int Columns = 3;
    private const int Rows    = 3;

    // ── Inspector ─────────────────────────────────────────────────────

    [Header("Goal target points (GoalArea children, row-major order)")]
    public Transform[] targetPoints;
    public Transform goalArea;

    [Header("References")]
    public BallController         ballController;
    public GoalkeeperController   goalkeeperController;
    public PlayerKickController   playerKickController;
    public ParticleSystem         goalParticles;
    public TargetSelectorUI       selectorUI;
    public Transform              playerTransform;
    public PenaltyResetController resetController;
    public GameEndUI              gameEndUI;

    [Header("Scene Transition")]
    [Tooltip("Name of the scene to load when all attempts are done. " +
             "Used only if GameEndUI is not assigned.")]
    public string scoreSceneName = "ScoreScene";

    [Header("3D World indicator")]
    public Transform worldIndicator;

    [Header("Attempt Configuration  (index 0 = attempt 1)")]
    public int maxAttempts = 5;

    [Tooltip("Ball launch speed for each attempt.")]
    public float[] ballSpeedsPerAttempt = { 4f, 5f, 6f, 6.5f, 7f };

    [Tooltip("Goalkeeper move speed for each attempt.")]
    public float[] keeperSpeedsPerAttempt = { 2f, 2.5f, 3.5f, 4.5f, 5f };

    [Tooltip("Probability [0-1] that the keeper guesses the correct column per attempt.")]
    public float[] keeperAccuracyPerAttempt = { 0.2f, 0.3f, 0.4f, 0.45f, 0.5f };

    [Header("Timing — fallback if no Animation Event fires")]
    public float shotAnimDelay = 1.2f;

    [Header("Ball Out-of-Play Timeout")]
    [Tooltip("Seconds after the ball is launched before forcing a reset if no trigger fired.")]
    public float ballTimeoutSeconds = 4f;

    // ── Runtime state ─────────────────────────────────────────────────

    private bool      isSelectingTarget = false;
    private bool      hasShotBeenFired  = false;
    private int       selectedIndex     = 4;

    private Transform pendingTarget;
    private Coroutine fallbackCoroutine;
    private Coroutine ballTimeoutCoroutine;

    /// <summary>Prevents duplicate outcome processing within a single turn.</summary>
    private bool outcomeHandled = false;

    // Attempt tracking
    private int   currentAttempt   = 0;   // 0-based
    private int   goalsScored      = 0;
    private float reactionAccuracy = 0.2f;

    // Player reset data
    private Vector3    playerStartPos;
    private Quaternion playerStartRot;

    private int Row => selectedIndex / Columns;
    private int Col => selectedIndex % Columns;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if ((targetPoints == null || targetPoints.Length == 0) && goalArea != null)
        {
            int count = goalArea.childCount;
            targetPoints = new Transform[count];
            for (int i = 0; i < count; i++)
                targetPoints[i] = goalArea.GetChild(i);
        }

        if (playerKickController != null)
            playerKickController.OnKickImpact += LaunchBall;

        SetWorldIndicator(false, Vector3.zero);
    }

    void Start()
    {
        // Reset global score at the start of a new game
        ScoreData.WhiteScore = 0;
        ScoreData.BlackScore = 0;

        // The snapshot of initial positions is taken by PenaltyResetController.Awake()
        // which runs before this Start(). playerTransform kept for legacy compatibility.
        if (playerTransform != null && resetController == null)
        {
            playerStartPos = playerTransform.position;
            playerStartRot = playerTransform.rotation;
        }

        ApplyAttemptSettings();
    }

    void OnDestroy()
    {
        if (playerKickController != null)
            playerKickController.OnKickImpact -= LaunchBall;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!hasShotBeenFired)
                ToggleSelectionMode();
            return;
        }

        if (!isSelectingTarget || hasShotBeenFired) return;

        HandleNavigation();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            ConfirmShot();
    }

    // ── Per-attempt configuration ─────────────────────────────────────

    /// <summary>Applies speeds and accuracy for the current attempt and updates the HUD.</summary>
    void ApplyAttemptSettings()
    {
        int idx = Mathf.Clamp(currentAttempt, 0, maxAttempts - 1);

        if (ballController != null && ballSpeedsPerAttempt.Length > idx)
            ballController.launchSpeed = ballSpeedsPerAttempt[idx];

        if (goalkeeperController != null && keeperSpeedsPerAttempt.Length > idx)
            goalkeeperController.moveSpeed = keeperSpeedsPerAttempt[idx];

        if (keeperAccuracyPerAttempt.Length > idx)
            reactionAccuracy = keeperAccuracyPerAttempt[idx];

        selectorUI?.UpdateAttemptDisplay(currentAttempt + 1, maxAttempts, goalsScored);

        Debug.Log($"[PenaltyTargetSelector] Intento {currentAttempt + 1}/{maxAttempts} " +
                  $"| balón={ballController?.launchSpeed} " +
                  $"| portero={goalkeeperController?.moveSpeed} " +
                  $"| precisión={reactionAccuracy:P0}");
    }

    // ── Selection mode ────────────────────────────────────────────────

    void ToggleSelectionMode()
    {
        isSelectingTarget = !isSelectingTarget;

        if (isSelectingTarget)
        {
            selectedIndex = 4;
            selectorUI?.ShowUI(true);
            selectorUI?.UpdateSelection(selectedIndex, targetPoints[selectedIndex].name);
            SetWorldIndicator(true, targetPoints[selectedIndex].position);
        }
        else
        {
            selectorUI?.ShowUI(false);
            SetWorldIndicator(false, Vector3.zero);
        }
    }

    // ── Navigation ────────────────────────────────────────────────────

    void HandleNavigation()
    {
        int newRow = Row;
        int newCol = Col;

        if      (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    newRow = Mathf.Max(0, Row - 1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  newRow = Mathf.Min(Rows - 1, Row + 1);

        if      (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  newCol = Mathf.Max(0, Col - 1);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) newCol = Mathf.Min(Columns - 1, Col + 1);

        int newIndex = newRow * Columns + newCol;
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            selectorUI?.UpdateSelection(selectedIndex, targetPoints[selectedIndex].name);
            SetWorldIndicator(true, targetPoints[selectedIndex].position);
        }
    }

    // ── Shot ─────────────────────────────────────────────────────────

    void ConfirmShot()
    {
        hasShotBeenFired  = true;
        isSelectingTarget = false;

        // Hide ALL HUD text immediately so nothing lingers during ball flight
        selectorUI?.HideAllUI();

        // Keep the world indicator on the target point while the ball is in flight
        Vector3 targetPos = targetPoints[selectedIndex].position;
        SetWorldIndicator(true, targetPos);

        pendingTarget = targetPoints[selectedIndex];

        int keeperIndex = ChooseGoalkeeperTarget(selectedIndex);
        goalkeeperController?.PrepareReaction(targetPoints[keeperIndex]);

        if (playerKickController != null)
        {
            playerKickController.PlayKick();
            fallbackCoroutine = StartCoroutine(FallbackLaunch());
        }
        else
        {
            StartCoroutine(TimedLaunch());
        }
    }

    IEnumerator FallbackLaunch()
    {
        yield return new WaitForSeconds(shotAnimDelay);
        LaunchBall();
    }

    IEnumerator TimedLaunch()
    {
        yield return new WaitForSeconds(shotAnimDelay);
        LaunchBall();
    }

    /// <summary>Launches the ball toward the pending target. Idempotent — fires only once per shot.</summary>
    private void LaunchBall()
    {
        if (pendingTarget == null || ballController == null) return;

        if (fallbackCoroutine != null)
        {
            StopCoroutine(fallbackCoroutine);
            fallbackCoroutine = null;
        }

        // LaunchToward adds an arc to compensate gravity so the ball actually crosses the goal line.
        ballController.LaunchToward(pendingTarget.position);
        pendingTarget = null;

        // Start timeout in case the ball escapes without hitting any trigger
        outcomeHandled = false;
        if (ballTimeoutCoroutine != null) StopCoroutine(ballTimeoutCoroutine);
        ballTimeoutCoroutine = StartCoroutine(BallTimeout());
    }

    /// <summary>
    /// Fires if neither GoalLineTrigger nor GoalKeeperSaveTrigger report an outcome
    /// within <see cref="ballTimeoutSeconds"/>. Counts the attempt as a miss and resets.
    /// </summary>
    IEnumerator BallTimeout()
    {
        yield return new WaitForSeconds(ballTimeoutSeconds);

        if (!outcomeHandled)
        {
            Debug.Log("[PenaltyTargetSelector] Timeout: el balón salió sin resultado. Contando como fallo.");
            OnGoalkeeperSaved();   // counts as a miss / keeper win
        }
    }

    // ── Goalkeeper target selection ───────────────────────────────────

    private int ChooseGoalkeeperTarget(int shotIndex)
    {
        int shotCol = shotIndex % Columns;

        int keeperCol;
        if (Random.value < reactionAccuracy)
        {
            keeperCol = shotCol;
        }
        else
        {
            int rand = Random.Range(0, Columns - 1);
            keeperCol = rand >= shotCol ? rand + 1 : rand;
        }

        int keeperRow = Random.Range(0, Rows);
        return keeperRow * Columns + keeperCol;
    }

    // ── Outcome callbacks ─────────────────────────────────────────────

    private const float ResetDelay  = 5f;   // unified delay for both goal and save
    private const float FinishDelay = 5f;

    /// <summary>Called by GoalLineTrigger when the ball crosses the goal line.</summary>
    public void OnGoalScored()
    {
        if (outcomeHandled) return;
        outcomeHandled = true;

        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }

        goalkeeperController?.Disarm();

        goalsScored++;
        ScoreData.WhiteScore = goalsScored;

        if (goalParticles != null)
            goalParticles.Play();

        currentAttempt++;

        if (currentAttempt >= maxAttempts)
            StartCoroutine(FinishGame(FinishDelay));
        else
            StartCoroutine(ResetForNextAttempt(ResetDelay));
    }

    /// <summary>Called by GoalKeeperSaveTrigger when the keeper stops the ball, or by BallTimeout on a miss.</summary>
    public void OnGoalkeeperSaved()
    {
        if (outcomeHandled) return;
        outcomeHandled = true;

        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }

        goalkeeperController?.Disarm();
        ScoreData.BlackScore++;

        currentAttempt++;

        if (currentAttempt >= maxAttempts)
            StartCoroutine(FinishGame(FinishDelay));
        else
            StartCoroutine(ResetForNextAttempt(ResetDelay));
    }

    // ── Reset flow ────────────────────────────────────────────────────

    IEnumerator ResetForNextAttempt(float delay)
    {
        // Wait, then stop particles first so they vanish before the actors move
        yield return new WaitForSeconds(delay - 0.5f);
        StopParticles();

        yield return new WaitForSeconds(0.5f);

        // Full positional + animation reset via PenaltyResetController (snapshot-based)
        if (resetController != null)
        {
            resetController.FullReset();
        }
        else
        {
            // Fallback: legacy path if PenaltyResetController is not wired
            ballController?.ResetBall();
            goalkeeperController?.ResetPosition();

            if (playerTransform != null)
            {
                playerTransform.position = playerStartPos;
                playerTransform.rotation = playerStartRot;
            }
            playerKickController?.ResetAnimator();
        }

        ApplyAttemptSettings();

        hasShotBeenFired  = false;
        outcomeHandled    = false;
        selectedIndex     = 4;
        isSelectingTarget = false;
        SetWorldIndicator(false, Vector3.zero);

        // Flash a visible message so the player knows a new attempt has started.
        // ShowNewAttemptFeedback internally restores the idle hint when it finishes.
        selectorUI?.ShowNewAttemptFeedback(currentAttempt + 1, maxAttempts);
    }

    IEnumerator FinishGame(float delay)
    {
        yield return new WaitForSeconds(delay - 0.5f);
        StopParticles();

        yield return new WaitForSeconds(0.5f);

        selectorUI?.HideAllUI();
        SetWorldIndicator(false, Vector3.zero);

        if (gameEndUI != null)
        {
            // Inline end-of-game panel
            gameEndUI.ShowResults(goalsScored, maxAttempts);
        }
        else
        {
            // Fallback: load the score scene directly
            Debug.Log($"[PenaltyTargetSelector] GameEndUI no asignado — cargando escena '{scoreSceneName}'");
            SceneManager.LoadScene(scoreSceneName);
        }
    }

    // ── Restart (called by GameEndUI.PlayAgain) ───────────────────────

    /// <summary>
    /// Resets the entire game to its initial state so the player can try again.
    /// Called from the "Jugar de nuevo" button via GameEndUI.
    /// </summary>
    public void RestartGame()
    {
        // Reset score counters
        currentAttempt = 0;
        goalsScored    = 0;
        ScoreData.WhiteScore = 0;
        ScoreData.BlackScore = 0;

        // Reset all actors to their initial positions
        resetController?.FullReset();

        // Restore first-attempt speeds and refresh HUD
        ApplyAttemptSettings();

        // Clear input state
        hasShotBeenFired  = false;
        outcomeHandled    = false;
        selectedIndex     = 4;
        isSelectingTarget = false;
        SetWorldIndicator(false, Vector3.zero);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void StopParticles()
    {
        if (goalParticles != null && goalParticles.isPlaying)
            goalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetWorldIndicator(bool visible, Vector3 worldPosition)
    {
        if (worldIndicator == null) return;
        worldIndicator.gameObject.SetActive(visible);
        if (visible) worldIndicator.position = worldPosition;
    }
}
