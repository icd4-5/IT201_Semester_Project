using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Controls the entrance -> game room -> spawn loop.
// Link WhackAMoleInteract.onGameStarted to StartGame() in the Inspector.
public class WhackAMoleSession : MonoBehaviour
{
    [Header("Player and spawn locations")]
    public Transform player;
    public Camera playerCamera;
    public Transform gameSpawnPoint;
    public Transform returnSpawnPoint;

    [Header("Connected components")]
    public WhackAMoleInteract entranceInteraction;
    public WhackAMoleGame moleGame;
    [Tooltip("Optional. If empty, finds FirstPersonWalker on Player automatically.")]
    public MonoBehaviour playerMovementScript;

    [Header("Timing")]
    [Min(1)] public int countdownSeconds = 5;
    [Min(5f)] public float roundDuration = 45f;
    [Min(0f)] public float resultsSeconds = 2f;
    [Min(1)] public int pointsPerHit = 10;

    [Header("Local high score (no wall sign)")]
    public string highScoreKey = "IT201_WhackAMole_HighScore";
    public float mouseSensitivity = 0.12f;

    private enum Phase { Idle, Countdown, Playing, Results }
    private Phase phase = Phase.Idle;
    private CharacterController controller;
    private bool previousMovementEnabled;
    private bool previousInteractionEnabled;
    private Quaternion originalCameraLocalRotation;
    private Vector3 fallbackReturnPosition;
    private Quaternion fallbackReturnRotation;
    private float pitch;
    private float secondsLeft;
    private int countdownNumber;
    private int score;
    private int highScore;
    private bool newRecord;
    private Coroutine sequence;

    public int Score => score;
    public int HighScore => highScore;
    public bool RoundIsRunning => phase == Phase.Playing;

    private void Awake()
    {
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    // Public method appears in the existing Hold-E script's UnityEvent dropdown.
    public void StartGame()
    {
        if (phase != Phase.Idle)
            return;

        if (player == null || playerCamera == null || gameSpawnPoint == null || moleGame == null)
        {
            Debug.LogError("WhackAMoleSession: Assign Player, Player Camera, Game Spawn Point, and Mole Game.", this);
            ResetEntrance();
            return;
        }

        if (!moleGame.IsConfigured())
        {
            Debug.LogError("WhackAMoleSession: Assign a Mole Prefab and at least one Hole Point on WhackAMoleGame.", moleGame);
            ResetEntrance();
            return;
        }

        if (playerMovementScript == null)
            playerMovementScript = player.GetComponent("FirstPersonWalker") as MonoBehaviour;

        if (playerMovementScript == null)
        {
            Debug.LogError("WhackAMoleSession: Can't find FirstPersonWalker. Drag the movement script into Player Movement Script.", this);
            ResetEntrance();
            return;
        }

        controller = player.GetComponent<CharacterController>();
        previousMovementEnabled = playerMovementScript.enabled;
        previousInteractionEnabled = entranceInteraction != null && entranceInteraction.enabled;
        originalCameraLocalRotation = playerCamera.transform.localRotation;
        fallbackReturnPosition = player.position;
        fallbackReturnRotation = player.rotation;
        score = 0;
        secondsLeft = roundDuration;
        newRecord = false;

        // Turning off the walker stops WASD, sprint, gravity, and jumping.
        // We provide mouse look ourselves during countdown and gameplay.
        playerMovementScript.enabled = false;
        if (entranceInteraction != null)
            entranceInteraction.enabled = false;

        TeleportPlayer(gameSpawnPoint.position, gameSpawnPoint.rotation);
        pitch = 0f;
        playerCamera.transform.localRotation = Quaternion.identity;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        phase = Phase.Countdown;
        sequence = StartCoroutine(CountdownAndRound());
    }

    private IEnumerator CountdownAndRound()
    {
        // 5, 4, 3, 2, 1 shown in the center of the Game view.
        for (int number = Mathf.Max(1, countdownSeconds); number >= 1; number--)
        {
            countdownNumber = number;
            yield return new WaitForSeconds(1f);
        }

        phase = Phase.Playing;
        moleGame.BeginRound(this, playerCamera);
        secondsLeft = roundDuration;

        while (phase == Phase.Playing && secondsLeft > 0f)
        {
            secondsLeft -= Time.deltaTime;
            yield return null;
        }

        if (phase == Phase.Playing)
            FinishRound();
    }

    // Called by WhackAMoleGame when the player successfully hits a mole.
    public void RegisterHit()
    {
        if (phase == Phase.Playing)
            score += Mathf.Max(1, pointsPerHit);
    }

    public void FinishRound()
    {
        if (phase != Phase.Playing)
            return;

        phase = Phase.Results;
        secondsLeft = 0f;
        moleGame.EndRound();
        newRecord = score > highScore;
        if (newRecord)
        {
            highScore = score;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (sequence != null)
            StopCoroutine(sequence);
        sequence = StartCoroutine(ShowResultsAndReturn());
    }

    private IEnumerator ShowResultsAndReturn()
    {
        if (resultsSeconds > 0f)
            yield return new WaitForSeconds(resultsSeconds);

        Vector3 destination = returnSpawnPoint != null ? returnSpawnPoint.position : fallbackReturnPosition;
        Quaternion direction = returnSpawnPoint != null ? returnSpawnPoint.rotation : fallbackReturnRotation;
        TeleportPlayer(destination, direction);
        playerCamera.transform.localRotation = originalCameraLocalRotation;
        if (playerMovementScript != null)
            playerMovementScript.enabled = previousMovementEnabled;

        phase = Phase.Idle;
        sequence = null;
        if (entranceInteraction != null)
        {
            entranceInteraction.ResetInteraction();
            entranceInteraction.enabled = previousInteractionEnabled;
        }
    }

    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled) controller.enabled = false;
        player.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
        if (wasEnabled) controller.enabled = true;
    }

    private void Update()
    {
        if (phase == Phase.Idle || player == null || playerCamera == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Click once to recapture mouse; this click won't hit a mole.
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            player.Rotate(0f, delta.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ResetEntrance()
    {
        if (entranceInteraction != null)
            entranceInteraction.ResetInteraction();
    }

    private void OnGUI()
    {
        if (phase == Phase.Idle)
            return;

        float factor = Mathf.Clamp(Screen.height / 720f, 0.75f, 1.5f);
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(27 * factor)
        };
        title.normal.textColor = Color.white;

        GUIStyle giant = new GUIStyle(title) { fontSize = Mathf.RoundToInt(100 * factor) };
        giant.normal.textColor = Color.white;

        GUIStyle body = new GUIStyle(title) { fontSize = Mathf.RoundToInt(19 * factor) };
        body.normal.textColor = Color.white;

        if (phase == Phase.Countdown || phase == Phase.Results)
        {
            float w = Mathf.Min(Screen.width - 30f, 460f * factor);
            float h = phase == Phase.Countdown ? 220f * factor : 170f * factor;
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Color old = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = old;

            if (phase == Phase.Countdown)
            {
                GUI.Label(new Rect(panel.x, panel.y + 11f * factor, w, 45f * factor), "GET READY", title);
                GUI.Label(new Rect(panel.x, panel.y + 60f * factor, w, 130f * factor), countdownNumber.ToString(), giant);
            }
            else
            {
                GUI.Label(new Rect(panel.x, panel.y + 12f * factor, w, 48f * factor),
                    newRecord ? "NEW HIGH SCORE!" : "ROUND COMPLETE", title);
                GUI.Label(new Rect(panel.x, panel.y + 65f * factor, w, 38f * factor),
                    "Score: " + score + "     Best: " + highScore, body);
                GUI.Label(new Rect(panel.x, panel.y + 108f * factor, w, 33f * factor),
                    "Returning to spawn...", body);
            }
        }
        else if (phase == Phase.Playing)
        {
            GUIStyle status = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20 * factor),
                fontStyle = FontStyle.Bold
            };
            status.normal.textColor = Color.white;
            GUI.Label(new Rect(16f, 12f, 340f * factor, 38f * factor),
                "Score: " + score + "    Time: " + Mathf.CeilToInt(secondsLeft), status);

            // Small aiming crosshair at the center of the screen.
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            Color old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 10f, y - 2f, 20f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 2f, y - 10f, 4f, 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x - 8f, y - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1f, y - 8f, 2f, 16f), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
