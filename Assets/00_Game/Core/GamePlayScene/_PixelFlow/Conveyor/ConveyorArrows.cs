using EventDispatcher;
using UnityEngine;

public class ConveyorArrows : MonoBehaviour
{
    public Transform[] arrows;
    public float speed = 1f;
    public float rotationSpeed = 360f;
    public float lastSegmentRotationMultiplier = 3f;

    [Header("Loop Boost")]
    public float loopMultiplier = 2f;
    private float currentMultiplier = 1f;

    private Vector3[] pathPositions;
    private Quaternion[] pathRotations;
    private float[] segmentLengths;
    private int[] targetIndex;
    private float[] segmentProgress;
    private int count;
    private int lastIdx;


    private bool _paused = false;

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public void Init()
    {
        OnStart();
        this.RegisterListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
    }
    void OnDestroy()
    {
        this.RemoveListener(EventID.LOOP_MODE_ENTERED, OnLoopEntered);
    }

    private void OnStart()
    {
        if (arrows == null || arrows.Length == 0)
        {
            arrows = new Transform[transform.childCount];
            for (int i = 0; i < arrows.Length; i++)
                arrows[i] = transform.GetChild(i);
        }

        count = arrows.Length;
        lastIdx = count - 1;
        pathPositions = new Vector3[count];
        pathRotations = new Quaternion[count];
        segmentLengths = new float[count];
        targetIndex = new int[count];
        segmentProgress = new float[count];

        for (int i = 0; i < count; i++)
        {
            pathPositions[i] = arrows[i].position;
            pathRotations[i] = arrows[i].rotation;
            targetIndex[i] = i == lastIdx ? 0 : i + 1;
        }

        for (int i = 0; i < count; i++)
        {
            int next = i == lastIdx ? 0 : i + 1;
            segmentLengths[i] = Vector3.Distance(pathPositions[i], pathPositions[next]);
        }
    }

    void Update()
    {
        if (_paused) return;
        float dt = Time.deltaTime;
        float speedDt = speed * currentMultiplier * dt;
        float rotDt = rotationSpeed * currentMultiplier * dt;
        float rotDtFast = rotDt * lastSegmentRotationMultiplier;

        for (int i = 0; i < count; i++)
        {
            int to = targetIndex[i];
            int from = to == 0 ? lastIdx : to - 1;

            float segLen = segmentLengths[from];
            if (segLen > 0.0001f)
                segmentProgress[i] += speedDt / segLen;

            while (segmentProgress[i] >= 1f)
            {
                segmentProgress[i] -= 1f;
                to = to == lastIdx ? 0 : to + 1;
                targetIndex[i] = to;
                from = to == 0 ? lastIdx : to - 1;
                segLen = segmentLengths[from];
            }

            int prev = from == 0 ? lastIdx : from - 1;
            int next = to == lastIdx ? 0 : to + 1;

            arrows[i].position = CatmullRom(
                pathPositions[prev],
                pathPositions[from],
                pathPositions[to],
                pathPositions[next],
                segmentProgress[i]
            );

            float currentRotDt = (from == 0 || from == 1) ? rotDtFast : rotDt;
            arrows[i].rotation = Quaternion.RotateTowards(arrows[i].rotation, pathRotations[to], currentRotDt);
        }
    }
    void OnLoopEntered(object _)
    {
        currentMultiplier = loopMultiplier;
    }
    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}