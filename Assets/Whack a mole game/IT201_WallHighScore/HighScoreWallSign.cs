using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HighScoreWallSign : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TextMesh displayText;

    [Header("Saved score (same key as WhackAMoleSession)")]
    [SerializeField] private string highScoreKey = "IT201_WhackAMole_HighScore";

    [Header("Update frequency")]
    [Min(0.05f)]
    [SerializeField] private float refreshEverySeconds = 0.25f;

    private int lastDisplayedScore = int.MinValue;
    private float nextRefreshTime;

    public void SetDisplay(TextMesh text)
    {
        displayText = text;
        RefreshDisplay(true);
    }

    private void OnEnable()
    {
        lastDisplayedScore = int.MinValue;
        nextRefreshTime = 0f;
        RefreshDisplay(true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RefreshDisplay(false);
            return;
        }

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + refreshEverySeconds;
        RefreshDisplay(false);
    }

    public void RefreshDisplay()
    {
        RefreshDisplay(true);
    }

    private void RefreshDisplay(bool force)
    {
        if (displayText == null)
            return;

        int best = Mathf.Max(0, PlayerPrefs.GetInt(highScoreKey, 0));
        if (!force && best == lastDisplayedScore)
            return;

        lastDisplayedScore = best;
        displayText.richText = true;
        displayText.color = Color.white;
        displayText.text = "High score: <color=#FF3030>" + best + "</color>";
    }
}
