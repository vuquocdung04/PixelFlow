using System.Collections.Generic;
using UnityEngine;

public class ConveyorArrows : MonoBehaviour
{
    public List<ConveyorArrow> conveyorArrows;
    public float speed = 1f;
    public float rotationSpeed = 360f;

    private List<Vector3> pathPositions;
    private List<Quaternion> pathRotations;
    private int[] targetIndex;
    private float[] segmentProgress;

    void Start()
    {
        conveyorArrows.AddRange(GetComponentsInChildren<ConveyorArrow>());

        int count = conveyorArrows.Count;
        pathPositions = new List<Vector3>(count);
        pathRotations = new List<Quaternion>(count);
        targetIndex = new int[count];
        segmentProgress = new float[count];

        for (int i = 0; i < count; i++)
        {
            pathPositions.Add(conveyorArrows[i].transform.position);
            pathRotations.Add(conveyorArrows[i].transform.rotation);
            targetIndex[i] = (i + 1) % count;
        }
    }

    void Update()
    {
        int count = pathPositions.Count;

        for (int i = 0; i < conveyorArrows.Count; i++)
        {
            Transform t = conveyorArrows[i].transform;
            int to = targetIndex[i];
            int from = (to - 1 + count) % count;

            float segLen = Vector3.Distance(pathPositions[from], pathPositions[to]);
            if (segLen > 0.0001f)
                segmentProgress[i] += (speed * Time.deltaTime) / segLen;

            while (segmentProgress[i] >= 1f)
            {
                segmentProgress[i] -= 1f;
                targetIndex[i] = (targetIndex[i] + 1) % count;
                to = targetIndex[i];
                from = (to - 1 + count) % count;
            }

            int prev = (from - 1 + count) % count;
            int next = (to + 1) % count;

            t.position = CatmullRom(
                pathPositions[prev],
                pathPositions[from],
                pathPositions[to],
                pathPositions[next],
                segmentProgress[i]
            );

            t.rotation = Quaternion.RotateTowards(t.rotation, pathRotations[to], rotationSpeed * Time.deltaTime);
        }
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
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