using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class WhackAMoleInteract : MonoBehaviour
{
    [Header("Scene References")]
    public Transform player;
    public Transform playHere;

    [Header("Interaction")]
    [Min(0.5f)] public float interactDistance = 2.5f;
    [Min(0.1f)] public float holdSeconds = 1f;

    [Header("When E Has Been Held")]
    public UnityEvent onGameStarted = new UnityEvent();

    private float holdProgress;
    private bool gameStarted;
    private float startedMessageUntil;
    private bool nearby;

    public bool GameStarted => gameStarted;

    private void Update()
    {
        if (gameStarted || player == null || playHere == null)
            return;

        Vector3 offset = player.position - playHere.position;
        offset.y = 0f;
        nearby = offset.sqrMagnitude <= interactDistance * interactDistance;

        if (nearby && Keyboard.current != null && Keyboard.current.eKey.isPressed)
        {
            holdProgress += Time.deltaTime;

            if (holdProgress >= holdSeconds)
            {
                gameStarted = true;
                nearby = false;
                startedMessageUntil = Time.time + 3f;
                Debug.Log("Whack-a-Mole game started!");
                onGameStarted.Invoke();
            }
        }
        else
        {
            holdProgress = 0f;
        }
    }

    public void ResetInteraction()
    {
        gameStarted = false;
        holdProgress = 0f;
        nearby = false;
    }

    private void OnGUI()
    {
        if (!gameStarted && (!nearby || player == null || playHere == null))
            return;

        if (gameStarted && Time.time >= startedMessageUntil)
            return;

        float width = Mathf.Min(360f, Screen.width - 30f);
        float height = 94f;
        Rect panel = new Rect((Screen.width - width) * 0.5f,
            Screen.height - height - 55f, width, height);

        Color oldColor = GUI.color;
        GUI.color = new Color(0.1f, 0.12f, 0.14f, 0.92f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUIStyle heading = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.026f), 17, 27),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle detail = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.017f), 12, 17),
            normal = { textColor = new Color(0.85f, 0.9f, 0.92f) }
        };

        if (gameStarted)
        {
            GUI.Label(new Rect(panel.x + 8f, panel.y + 17f, width - 16f, 32f),
                "GAME STARTED!", heading);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 51f, width - 16f, 24f),
                "Get ready to whack the moles", detail);
        }
        else
        {
            GUI.Label(new Rect(panel.x + 8f, panel.y + 7f, width - 16f, 35f),
                "Hold E to Play", heading);

            Rect bar = new Rect(panel.x + 20f, panel.y + 54f, width - 40f, 14f);
            GUI.color = new Color(0.28f, 0.30f, 0.34f, 1f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            float fraction = Mathf.Clamp01(holdProgress / holdSeconds);
            GUI.color = new Color(0.35f, 0.85f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height),
                Texture2D.whiteTexture);
        }

        GUI.color = oldColor;
    }
}
