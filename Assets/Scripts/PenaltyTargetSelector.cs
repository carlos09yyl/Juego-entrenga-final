using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central controller for the 5-attempt penalty system.
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
    public Transform   goalArea;

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
    [Tooltip("Scene to load when all attempts are done if GameEndUI is not assigned.")]
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

    [Header("Timing")]
    [Tooltip("Seconds to wait for the kick animation before forcing ball launch.")]
    public float shotAnimDelay = 1.2f;

    [Header("Ball Out-of-Play Timeout")]
    [Tooltip("Seconds after ball launch before forcing a miss if no trigger fired.")]
    public float ballTimeoutSeconds = 5f;

    [Header("Dynamic Speed (based on selection time)")]
    [Tooltip("Select within this many seconds → full max speed.")]
    public float fastSelectionThreshold = 1.5f;
    [Tooltip("Take this many seconds or more → minimum speed.")]
    public float slowSelectionThreshold = 5f;
    [Tooltip("Minimum ball speed regardless of selection time.")]
    public float minBallSpeed = 3f;

    // ── Turn state machine ────────────────────────────────────────────
    // The game is always in exactly one of these phases.
    private enum TurnPhase
    {
        WaitingForInput,   // player can press Enter to open the grid
        Selecting,         // grid is open; player navigates and confirms
        BallInFlight,      // ball has been launched; waiting for trigger or timeout
        ResettingForNext,  // reset coroutine running; all input locked
        GameOver           // no more turns
    }

    private TurnPhase phase = TurnPhase.WaitingForInput;

    // ── Runtime state ─────────────────────────────────────────────────
    private int   selectedIndex     = 4;
    private int   currentAttempt    = 0;   // 0-based
    private int   goalsScored       = 0;
    private float reactionAccuracy  = 0.2f;
    private float selectionStartTime = 0f;
    private float currentMaxBallSpeed = 6f;

    private Transform pendingTarget;
    private Coroutine fallbackCoroutine;
    private Coroutine ballTimeoutCoroutine;
    private Coroutine resetCoroutine;

    // Player reset data (fallback when no PenaltyResetController is wired)
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
            playerKickController.OnKickImpact += OnAnimationKickImpact;

        SetWorldIndicator(false, Vector3.zero);
    }

    void Start()
    {
        ScoreData.WhiteScore = 0;
        ScoreData.BlackScore = 0;

        if (playerTransform != null && resetController == null)
        {
            playerStartPos = playerTransform.position;
            playerStartRot = playerTransform.rotation;
        }

        ApplyAttemptSettings();
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowIdleHint();
    }

    void OnDestroy()
    {
        if (playerKickController != null)
            playerKickController.OnKickImpact -= OnAnimationKickImpact;
    }

    void Update()
    {
        switch (phase)
        {
            case TurnPhase.WaitingForInput:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OpenSelectionGrid();
                break;

            case TurnPhase.Selecting:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    CloseSelectionGrid();
                    break;
                }
                HandleNavigation();
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    ConfirmShot();
                break;

            // BallInFlight, ResettingForNext, GameOver → no input accepted
        }
    }

    // ── Per-attempt configuration ─────────────────────────────────────

    /// <summary>Applies speeds / accuracy for currentAttempt and refreshes the always-visible HUD.</summary>
    void ApplyAttemptSettings()
    {
        int idx = Mathf.Clamp(currentAttempt, 0, maxAttempts - 1);

        if (ballSpeedsPerAttempt.Length > idx)
            currentMaxBallSpeed = ballSpeedsPerAttempt[idx];

        if (goalkeeperController != null && keeperSpeedsPerAttempt.Length > idx)
            goalkeeperController.moveSpeed = keeperSpeedsPerAttempt[idx];

        if (keeperAccuracyPerAttempt.Length > idx)
            reactionAccuracy = keeperAccuracyPerAttempt[idx];

        // Always update the persistent counters (attempt + goals) — these should never disappear.
        RefreshPersistentHUD();

        Debug.Log($"[PTS] Intento {currentAttempt + 1}/{maxAttempts} " +
                  $"| maxVel={currentMaxBallSpeed} | portero={goalkeeperController?.moveSpeed} " +
                  $"| precisión={reactionAccuracy:P0}");
    }

    /// <summary>
    /// Updates only the always-visible attempt / goals counters.
    /// Safe to call at any time — these labels must never be hidden during gameplay.
    /// </summary>
    void RefreshPersistentHUD()
    {
        selectorUI?.UpdateAttemptDisplay(currentAttempt + 1, maxAttempts, goalsScored);
    }

    // ── Selection grid ────────────────────────────────────────────────

    void OpenSelectionGrid()
    {
        phase = TurnPhase.Selecting;
        selectionStartTime = Time.time;
        selectedIndex = 4;
        selectorUI?.ShowUI(true);
        selectorUI?.UpdateSelection(selectedIndex, targetPoints[selectedIndex].name);
        SetWorldIndicator(true, targetPoints[selectedIndex].position);
    }

    void CloseSelectionGrid()
    {
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowUI(false);
        SetWorldIndicator(false, Vector3.zero);
        selectorUI?.ShowIdleHint();
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

    // ── Shot ──────────────────────────────────────────────────────────

    void ConfirmShot()
    {
        // Hide selection grid and world indicator immediately
        selectorUI?.ShowUI(false);
        SetWorldIndicator(false, Vector3.zero);
        // Keep attempt/goals labels visible — do NOT call HideAllUI here
        selectorUI?.HideSelectionOnlyUI();

        // Dynamic launch speed based on reaction time
        float selectionDuration = Time.time - selectionStartTime;
        float t            = Mathf.InverseLerp(fastSelectionThreshold, slowSelectionThreshold, selectionDuration);
        float dynamicSpeed = Mathf.Lerp(currentMaxBallSpeed, minBallSpeed, t);

        if (ballController != null)
            ballController.launchSpeed = dynamicSpeed;

        Debug.Log($"[PTS] Selección en {selectionDuration:F1}s → vel={dynamicSpeed:F1}");

        pendingTarget = targetPoints[selectedIndex];

        int keeperIndex = ChooseGoalkeeperTarget(selectedIndex);
        goalkeeperController?.PrepareReaction(targetPoints[keeperIndex]);

        // Transition to BallInFlight BEFORE starting coroutines
        phase = TurnPhase.BallInFlight;

        if (playerKickController != null)
        {
            playerKickController.PlayKick();
            fallbackCoroutine = StartCoroutine(FallbackLaunch());
        }
        else
        {
            fallbackCoroutine = StartCoroutine(FallbackLaunch());
        }
    }

    IEnumerator FallbackLaunch()
    {
        yield return new WaitForSeconds(shotAnimDelay);
        LaunchBall();
    }

    // Called by PlayerKickController animation event
    private void OnAnimationKickImpact()
    {
        if (phase == TurnPhase.BallInFlight)
            LaunchBall();
    }

    /// <summary>Launches the ball. Idempotent — safe to call from animation event AND fallback.</summary>
    private void LaunchBall()
    {
        if (pendingTarget == null || ballController == null) return;

        // Cancel the fallback coroutine if the animation event already fired
        if (fallbackCoroutine != null)
        {
            StopCoroutine(fallbackCoroutine);
            fallbackCoroutine = null;
        }

        Vector3 target = pendingTarget.position;
        pendingTarget = null;

        ballController.LaunchToward(target);

        // Cancel any stale timeout from a previous turn (safety net)
        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }
        ballTimeoutCoroutine = StartCoroutine(BallTimeout());
    }

    /// <summary>Forces a miss if the ball leaves the field without hitting any trigger.</summary>
    IEnumerator BallTimeout()
    {
        yield return new WaitForSeconds(ballTimeoutSeconds);

        // Only fire if we are still waiting for an outcome
        if (phase == TurnPhase.BallInFlight)
        {
            Debug.Log("[PTS] Timeout: balón fuera sin resultado → fallo");
            ProcessOutcome(isGoal: false);
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
            int rand  = Random.Range(0, Columns - 1);
            keeperCol = rand >= shotCol ? rand + 1 : rand;
        }

        int keeperRow = Random.Range(0, Rows);
        return keeperRow * Columns + keeperCol;
    }

    // ── Outcome callbacks ─────────────────────────────────────────────

    /// <summary>Called by GoalLineTrigger when the ball crosses the goal line.</summary>
    public void OnGoalScored()
    {
        if (phase != TurnPhase.BallInFlight) return;
        Debug.Log("[PTS] GOL confirmado");
        ProcessOutcome(isGoal: true);
    }

    /// <summary>Called by GoalKeeperSaveTrigger when the keeper contacts the ball.</summary>
    public void OnGoalkeeperSaved()
    {
        if (phase != TurnPhase.BallInFlight) return;
        Debug.Log("[PTS] ATAJADA confirmada");
        ProcessOutcome(isGoal: false);
    }

    // ── Central outcome handler ───────────────────────────────────────

    /// <summary>
    /// Single entry point for all outcomes (goal, save, timeout).
    /// Only accepted while phase == BallInFlight — the phase check is the gate.
    /// </summary>
    private void ProcessOutcome(bool isGoal)
    {
        // Immediately move out of BallInFlight so no second outcome can sneak in
        phase = TurnPhase.ResettingForNext;

        // Stop the timeout coroutine
        if (ballTimeoutCoroutine != null)
        {
            StopCoroutine(ballTimeoutCoroutine);
            ballTimeoutCoroutine = null;
        }

        goalkeeperController?.Disarm();

        if (isGoal)
        {
            goalsScored++;
            ScoreData.WhiteScore = goalsScored;

            if (goalParticles != null)
                goalParticles.Play();
        }
        else
        {
            ScoreData.BlackScore++;
        }

        // Update HUD immediately so the player sees the result
        currentAttempt++;
        RefreshPersistentHUD();

        Debug.Log($"[PTS] Intento {currentAttempt}/{maxAttempts} completo — Goles: {goalsScored}");

        if (currentAttempt >= maxAttempts)
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(FinishGame());
        }
        else
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(ResetForNextAttempt());
        }
    }

    // ── Reset flow ────────────────────────────────────────────────────

    private const float ResetDelay  = 3.5f;
    private const float FinishDelay = 3.5f;

    IEnumerator ResetForNextAttempt()
    {
        yield return new WaitForSeconds(ResetDelay - 0.5f);
        StopParticles();
        yield return new WaitForSeconds(0.5f);

        // Restore all actor positions
        if (resetController != null)
        {
            resetController.FullReset();
        }
        else
        {
            ballController?.ResetBall();
            goalkeeperController?.ResetPosition();

            if (playerTransform != null)
            {
                playerTransform.position = playerStartPos;
                playerTransform.rotation = playerStartRot;
            }
            playerKickController?.ResetAnimator();
        }

        // Apply new attempt settings BEFORE showing feedback
        ApplyAttemptSettings();

        SetWorldIndicator(false, Vector3.zero);

        // Show "Intento X de Y" flash, then restore idle hint
        selectorUI?.ShowNewAttemptFeedback(currentAttempt + 1, maxAttempts);

        // Allow input again
        phase = TurnPhase.WaitingForInput;

        resetCoroutine = null;
    }

    IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(FinishDelay - 0.5f);
        StopParticles();
        yield return new WaitForSeconds(0.5f);

        phase = TurnPhase.GameOver;
        SetWorldIndicator(false, Vector3.zero);
        selectorUI?.HideSelectionOnlyUI();

        if (gameEndUI != null)
        {
            gameEndUI.ShowResults(goalsScored, maxAttempts);
        }
        else
        {
            Debug.Log($"[PTS] GameEndUI no asignado → cargando '{scoreSceneName}'");
            SceneManager.LoadScene(scoreSceneName);
        }

        resetCoroutine = null;
    }

    // ── Restart (called by GameEndUI) ─────────────────────────────────

    /// <summary>Resets the full game to attempt 1. Called from GameEndUI.PlayAgain().</summary>
    public void RestartGame()
    {
        currentAttempt = 0;
        goalsScored    = 0;
        ScoreData.WhiteScore = 0;
        ScoreData.BlackScore = 0;

        resetController?.FullReset();
        ApplyAttemptSettings();

        SetWorldIndicator(false, Vector3.zero);
        phase = TurnPhase.WaitingForInput;
        selectorUI?.ShowIdleHint();
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
