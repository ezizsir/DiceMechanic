using System;
using System.Collections;
using UnityEngine;

public class DieRoller : MonoBehaviour
{
    [Header("Animation Settings:")]
    [SerializeField] private float riseHeight = 0.5f;   // how far up the whole die lifts before shuffling
    [SerializeField] private float moveDuration = 0.25f;// raise/lower animation time
    [SerializeField] private float shuffleDelay = 0.1f; // seconds between face swaps
    [SerializeField] private int dieFaces = 6;

    [Header("Layer Settings:")]
    [SerializeField] private float layerStep = 0.1f;

    [Header("Lock Settings:")]
    [SerializeField] private float lockedAlpha = 0.45f; // how faded a locked die looks

    private Transform[] move;
    private Vector3[] localCoordinate;
    private SpriteRenderer[] faceRenderers;
    private bool isRolling = false;
    private int topFace = 0;
    private float layerOffset = 0f;
    private bool isLocked = false;

    public event Action<DieRoller> OnRollEnd;

    public int FaceValue => topFace + 1;   // topFace is 0-5, face value is 1-6
    public bool IsRolling => isRolling;
    public bool IsLocked => isLocked;
    // Manager sets this true after a roll so clicks only lock dice between rolls
    public bool CanLock { get; set; } = false;

    private void Awake()
    {
        move = new Transform[dieFaces];
        localCoordinate = new Vector3[dieFaces];
        faceRenderers = new SpriteRenderer[dieFaces];

        for (int i = 0; i < dieFaces; i++)
        {
            move[i] = null;
        }

        // Hierarchy order is NOT trusted (children can be in any order).
        // Instead, each child is matched by name: "Die_Faces3_..." goes to slot 2 (face 3).
        foreach (Transform child in transform)
        {
            for (int f = 1; f <= dieFaces; f++)
            {
                if (child.name.StartsWith($"Die_Faces{f}"))
                {
                    move[f - 1] = child;
                    localCoordinate[f - 1] = child.localPosition;
                    faceRenderers[f - 1] = child.GetComponent<SpriteRenderer>();
                    break;
                }
            }
        }

        // Safety check: complain loudly if a face slot never got filled
        for (int i = 0; i < dieFaces; i++)
        {
            if (move[i] == null)
            {
                Debug.LogError($"{name}: no child named 'Die_Faces{i + 1}...' found. Face {i + 1} will not work.");
            }
        }
    }

    // Called by the manager: lift the whole die (all faces move together as one unit)
    public void RaiseUp()
    {
        StartCoroutine(RaiseRoutine());
    }

    // Called by the manager: shuffle N times, then drop back down, then report done
    public void BeginShuffle(int shuffleCount)
    {
        if (!isRolling)
        {
            StartCoroutine(ShuffleRoutine(shuffleCount));
        }
    }

    // Reset face z-order (flatten the stack) without touching the die's position
    public void ResetLayers()
    {
        layerOffset = 0f;

        for (int i = 0; i < dieFaces; i++)
        {
            move[i].localPosition = new Vector3(localCoordinate[i].x, localCoordinate[i].y, 0f);
        }
    }

    // Player click: lock/unlock this die (only allowed between rolls)
    private void OnMouseDown()
    {
        if (!CanLock || isRolling) return;

        isLocked = !isLocked;
        SetFaceAlpha(isLocked ? lockedAlpha : 1f);
    }

    private void SetFaceAlpha(float alpha)
    {
        for (int i = 0; i < dieFaces; i++)
        {
            Color c = faceRenderers[i].color;
            c.a = alpha;
            faceRenderers[i].color = c;
        }
    }

    private IEnumerator RaiseRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * riseHeight;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    private IEnumerator ShuffleRoutine(int shuffleCount)
    {
        isRolling = true;

        // Shuffling: pick a NEW random face each tick so every tick is visible
        for (int i = 0; i < shuffleCount; i++)
        {
            RandomizeLayers();
            yield return new WaitForSeconds(shuffleDelay);
        }

        // Drop back down (lerp only the Y axis, keep the shuffled z-offset intact)
        Vector3 start = transform.position;
        Vector3 end = start - Vector3.up * riseHeight;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        isRolling = false;
        OnRollEnd?.Invoke(this);
    }

    // Pick a NEW top face (avoids re-picking the current one, which made shuffling look dead)
    private void RandomizeLayers()
    {
        int randomIndex = UnityEngine.Random.Range(0, dieFaces); // 1-6
        while (randomIndex == topFace) randomIndex = UnityEngine.Random.Range(0, dieFaces);

        layerOffset -= layerStep;

        move[randomIndex].localPosition = new Vector3(localCoordinate[randomIndex].x, localCoordinate[randomIndex].y, layerOffset);
        topFace = randomIndex;
        Debug.Log($"Die {gameObject.name} top face: {topFace + 1}");
    }
}
