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
    public TextMeshProUGUI attemptLabel;     // "Intento 1/3"
    public TextMeshProUGUI goalsLabel;       // "⚽ 1"

    // ─────────────────────────────────────────────────────────────────

    private const string HintSelect = "WASD / Flechas: navegar   |   Space / Click: disparar";
    private const string HintIdle   = "Presiona <b>Enter</b> para seleccionar destino";

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

    // ── Helpers ───────────────────────────────────────────────────────

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
