
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WhackAMoleGame : MonoBehaviour
{
    [Header("Assign the empty parent with 9 children")]
    public Transform holesParent;
    public GameObject molePrefab;

    [Header("Mole movement")]
    public float hiddenDepth = 1f;
    public float visibleHeight = 0.05f;
    public float riseTime = 0.18f;
    public float visibleTime = 0.9f;
    public float fallTime = 0.18f;
    public float timeBetweenMoles = 0.2f;
    public float moleScale = 1f;

    [Header("Clicking")]
    public float maxHitDistance = 25f;

    private readonly List<Transform> holes = new List<Transform>();
    private WhackAMoleSession session;
    private Camera gameCamera;
    private GameObject activeMole;
    private Coroutine moleRoutine;
    private bool roundActive;
    private bool moleCanBeHit;
    private bool moleWasHit;
    private int lastHole = -1;

    public bool IsConfigured()
    {
        FindHoles();
        return holes.Count > 0 && molePrefab != null;
    }

    private void FindHoles()
    {
        holes.Clear();

        if (holesParent == null)
            return;

        for (int i = 0; i < holesParent.childCount; i++)
            holes.Add(holesParent.GetChild(i));
    }

    public void BeginRound(WhackAMoleSession owner, Camera cameraToUse)
    {
        EndRound();

        if (!IsConfigured() || owner == null || cameraToUse == null)
        {
            Debug.LogError(
                "WhackAMoleGame: Assign Holes Parent, Mole Prefab, and Player Camera.",
                this);
            return;
        }

        session = owner;
        gameCamera = cameraToUse;
        lastHole = -1;

        activeMole = Instantiate(molePrefab);
        activeMole.name = "ActiveMole";
        activeMole.transform.localScale *= Mathf.Max(0.01f, moleScale);
        AddHitColliderIfNeeded(activeMole);
        activeMole.SetActive(false);

        roundActive = true;
        moleRoutine = StartCoroutine(RunMoles());
    }

    public void EndRound()
    {
        roundActive = false;
        moleCanBeHit = false;
        moleWasHit = false;

        if (moleRoutine != null)
        {
            StopCoroutine(moleRoutine);
            moleRoutine = null;
        }

        if (activeMole != null)
        {
            Destroy(activeMole);
            activeMole = null;
        }
    }

    private void OnDisable()
    {
        EndRound();
    }

    private void Update()
    {
        if (!roundActive || !moleCanBeHit || activeMole == null ||
            gameCamera == null || Mouse.current == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Physics.SyncTransforms();

        Ray ray = new Ray(
            gameCamera.transform.position,
            gameCamera.transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(
            ray, maxHitDistance, ~0, QueryTriggerInteraction.Collide);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform target = hit.collider.transform;

            if (target == activeMole.transform ||
                target.IsChildOf(activeMole.transform))
            {
                moleWasHit = true;
                moleCanBeHit = false;
                session.RegisterHit();
                return;
            }

            // Ignore the board collider covering the holes.
            if (target == transform || target.IsChildOf(transform))
                continue;

            // Walls still block hits.
            if (!hit.collider.isTrigger)
                return;
        }
    }

    private IEnumerator RunMoles()
    {
        while (roundActive)
        {
            int nextHole = UnityEngine.Random.Range(0, holes.Count);

            if (holes.Count > 1 && nextHole == lastHole)
            {
                nextHole = (nextHole +
                    UnityEngine.Random.Range(1, holes.Count)) % holes.Count;
            }

            lastHole = nextHole;
            Transform hole = holes[nextHole];

            Vector3 hiddenPosition =
                hole.position - Vector3.up * hiddenDepth;

            Vector3 shownPosition =
                hole.position + Vector3.up * visibleHeight;

            activeMole.transform.SetPositionAndRotation(
                hiddenPosition, hole.rotation);

            activeMole.SetActive(true);
            moleCanBeHit = false;
            moleWasHit = false;

            yield return AnimateMole(
                hiddenPosition, shownPosition, riseTime);

            if (!roundActive)
                yield break;

            moleCanBeHit = true;
            float remaining = visibleTime;

            while (roundActive && !moleWasHit && remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            moleCanBeHit = false;

            yield return AnimateMole(
                activeMole.transform.position,
                hiddenPosition,
                fallTime);

            if (!roundActive)
                yield break;

            activeMole.SetActive(false);

            if (timeBetweenMoles > 0f)
                yield return new WaitForSeconds(timeBetweenMoles);
        }
    }

    private IEnumerator AnimateMole(
        Vector3 from, Vector3 to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (roundActive && elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration));

            activeMole.transform.position = Vector3.Lerp(from, to, t);

            yield return null;
        }

        if (roundActive && activeMole != null)
            activeMole.transform.position = to;
    }

    private static void AddHitColliderIfNeeded(GameObject mole)
    {
        if (mole.GetComponentInChildren<Collider>() != null)
            return;

        Renderer[] renderers = mole.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            BoxCollider fallback = mole.AddComponent<BoxCollider>();
            fallback.center = new Vector3(0f, 0.5f, 0f);
            fallback.size = Vector3.one;
            fallback.isTrigger = true;
            return;
        }

        Bounds combined = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        Vector3 localCenter =
            mole.transform.InverseTransformPoint(combined.center);

        Vector3 worldSize = combined.size;
        Vector3 scale = mole.transform.lossyScale;

        BoxCollider collider = mole.AddComponent<BoxCollider>();
        collider.center = localCenter;

        collider.size = new Vector3(
            worldSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            worldSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            worldSize.z / Mathf.Max(0.001f, Mathf.Abs(scale.z))
        );

        collider.isTrigger = true;
    }
}
