using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the penalty target selection HUD.
/// Shows a 3×3 grid, highlights the selected cell, and displays attempt / goal info.
/// </summary>
public class TargetSelectorUI : MonoBehaviour
{
    [Header("Root panel (shown / hidden during selection)")]
    public GameObject panel;

    [Header("3×3 Cell images — row-major order (UpLeft … DowRight)")]
    public Image[] cellImages;

    [Header("Colors")]
    public Color normalColor   = new Color(1f, 1f, 1f, 0.3f);
    public Color selectedColor = new Color(1f, 0.85f, 0f, 0.85f);

    [Header("Labels")]
    public TextMeshProUGUI selectionLabel;   // selected target name
    public TextMeshProUGUI hintLabel;        // always-visible hint
    public TextMeshProUGUI attemptLabel;     // "Intento 1/5"
    public TextMeshProUGUI goalsLabel;       // "⚽ 1"

    [Header("New-attempt feedback")]
    [Tooltip("Label used to flash the 'new attempt' message. Can reuse hintLabel.")]
    public TextMeshProUGUI feedbackLabel;
    [Tooltip("How long the feedback message stays visible.")]
    public float feedbackDuration = 2f;

    // ─────────────────────────────────────────────────────────────────

    private const string HintSelect = "WASD / Flechas: navegar   |   Space / Click: disparar";
    private const string HintIdle   = "Presiona <b>Enter</b> para seleccionar destino";

    private Coroutine feedbackCoroutine;

    void Awake()
    {
        ShowUI(false);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Shows or hides the selection panel and updates the hint text.</summary>
    public void ShowUI(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);

        if (hintLabel != null)
        {
            hintLabel.text = visible ? HintSelect : HintIdle;
            // Always keep hint visible so the idle prompt is shown between shots.
            hintLabel.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides every HUD element completely (selection panel, hint, feedback).
    /// The persistent attempt/goals labels are preserved — call this only at game over.
    /// </summary>
    public void HideAllUI()
    {
        if (panel != null)
            panel.SetActive(false);

        if (hintLabel != null)
            hintLabel.gameObject.SetActive(false);

        if (selectionLabel != null)
            selectionLabel.gameObject.SetActive(false);

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (feedbackLabel != null && feedbackLabel != hintLabel)
            feedbackLabel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Hides the selection grid and hint text, but keeps the attempt counter and
    /// goals label visible so the player always sees the current score.
    /// Call this when the ball is launched or the grid is closed.
    /// </summary>
    public void HideSelectionOnlyUI()
    {
        if (panel != null)
            panel.SetActive(false);

        if (hintLabel != null)
            hintLabel.gameObject.SetActive(false);

        if (selectionLabel != null)
            selectionLabel.gameObject.SetActive(false);

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (feedbackLabel != null && feedbackLabel != hintLabel)
            feedbackLabel.gameObject.SetActive(false);

        // attemptLabel and goalsLabel are intentionally NOT hidden here.
    }

    /// <summary>Restores the idle hint label after the shot resolves.</summary>
    public void ShowIdleHint()
    {
        if (hintLabel != null)
        {
            hintLabel.text = HintIdle;
            hintLabel.gameObject.SetActive(true);
        }
    }

    /// <summary>Highlights the selected cell and updates the target-name label.</summary>
    public void UpdateSelection(int index, string targetName)
    {
        for (int i = 0; i < cellImages.Length; i++)
        {
            if (cellImages[i] == null) continue;
            cellImages[i].color = (i == index) ? selectedColor : normalColor;
        }

        if (selectionLabel != null)
            selectionLabel.text = FormatTargetName(targetName);
    }

    /// <summary>Refreshes the attempt counter and goals tally shown in the HUD.</summary>
    public void UpdateAttemptDisplay(int attemptNumber, int maxAttempts, int goals)
    {
        if (attemptLabel != null)
            attemptLabel.text = $"Intento {attemptNumber}/{maxAttempts}";

        if (goalsLabel != null)
            goalsLabel.text = $"⚽ {goals}";
    }

    /// <summary>
    /// Flashes a temporary message to signal the start of a new attempt.
    /// Uses <see cref="feedbackLabel"/> if assigned, otherwise falls back to <see cref="hintLabel"/>.
    /// </summary>
    public void ShowNewAttemptFeedback(int attemptNumber, int maxAttempts)
    {
        TextMeshProUGUI target = feedbackLabel != null ? feedbackLabel : hintLabel;
        if (target == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FlashFeedback(target, attemptNumber, maxAttempts));
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private IEnumerator FlashFeedback(TextMeshProUGUI label, int attemptNumber, int maxAttempts)
    {
        string originalText  = label.text;
        Color  originalColor = label.color;

        label.text  = $"¡Intento {attemptNumber} de {maxAttempts}!\nPresiona <b>Enter</b> para apuntar";
        label.color = new Color(1f, 0.9f, 0f, 1f);   // bright yellow
        label.gameObject.SetActive(true);

        yield return new WaitForSeconds(feedbackDuration);

        // Restore hint text and color
        label.text  = HintIdle;
        label.color = originalColor;

        feedbackCoroutine = null;
    }

    private string FormatTargetName(string raw) => raw switch
    {
        "UpLeft"    => "Arriba Izquierda",
        "UpMiddle"  => "Arriba Centro",
        "UpRight"   => "Arriba Derecha",
        "MidLeft"   => "Centro Izquierda",
        "Middle"    => "Centro",
        "MidRight"  => "Centro Derecha",
        "Dowleft"   => "Abajo Izquierda",
        "DowMiddle" => "Abajo Centro",
        "DowRight"  => "Abajo Derecha",
        _           => raw
    };
}
