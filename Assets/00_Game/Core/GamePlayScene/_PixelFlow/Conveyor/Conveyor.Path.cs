using System.Collections.Generic;
using UnityEngine;

public partial class Conveyor
{
    [Space(10), Header("Item Path")]
    public List<Transform> itemPath;
    public float itemPathSpeed = 10f;

    void SendSlotAlongPath(ItemSlot slot, Shooter shooter)
    {
        int count = itemPath.Count;
        if (count < 2)
        {
            OnSlotPathComplete(slot, shooter);
            return;
        }

        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
            points[i] = itemPath[i].position;

        float length = 0f;
        for (int i = 0; i < count - 1; i++)
            length += Vector3.Distance(points[i], points[i + 1]);

        float duration = itemPathSpeed > 0.0001f ? length / itemPathSpeed : 0f;

        slot.MoveAlongPath(points, duration, () => OnSlotPathComplete(slot, shooter));
    }

    void OnSlotPathComplete(ItemSlot slot, Shooter shooter)
    {
        DispatchShooterToWaitArea(shooter);
        ReturnSlot(slot);
    }

    void DispatchShooterToWaitArea(Shooter shooter)
    {
        WaitArea empty = WaitAreaController.Instance.FindEmptyWaitArea();
        if (empty == null) return;
        shooter.SetAnimState(Shooter.AnimState.Idle);
        empty.AddOccupant(shooter);
        shooter.transform.SetParent(empty.transform);
        shooter.transform.localScale = Vector3.one;
        shooter.transform.rotation = Quaternion.identity;
        shooter.JumpTo(empty.transform.position);
    }

    void OnDrawGizmos()
    {
        DrawQueueGizmos();
        DrawPathGizmos();
    }

    void DrawQueueGizmos()
    {
        if (firstPoint == null || returnPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(firstPoint.position, 0.12f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(returnPoint.position, 0.12f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firstPoint.position, returnPoint.position);

        float previewSpacing = initialItemCount > 1
            ? Mathf.Abs(returnPoint.position.x - firstPoint.position.x) / (initialItemCount - 1)
            : spacing;
        float dir = Mathf.Sign(returnPoint.position.x - firstPoint.position.x);
        if (dir == 0f) dir = 1f;

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.7f);
        for (int i = 0; i < initialItemCount; i++)
        {
            Vector3 p = firstPoint.position;
            p.x = firstPoint.position.x + i * previewSpacing * dir;
            Gizmos.DrawWireSphere(p, 0.12f);
        }
    }

    void DrawPathGizmos()
    {
        if (itemPath == null || itemPath.Count == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < itemPath.Count; i++)
        {
            if (itemPath[i] == null) continue;
            Gizmos.DrawSphere(itemPath[i].position, 0.08f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(itemPath[i].position + Vector3.up * 0.2f, $"[{i}]");
#endif
        }

        if (itemPath.Count < 2) return;

        Vector3[] pts = new Vector3[itemPath.Count];
        for (int i = 0; i < itemPath.Count; i++)
        {
            if (itemPath[i] == null) return;
            pts[i] = itemPath[i].position;
        }

        Gizmos.color = Color.white;
        const int segs = 30;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? pts[i] + (pts[i] - pts[i + 1]) : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i + 2 < pts.Length ? pts[i + 2] : pts[i + 1] + (pts[i + 1] - pts[i]);

            Vector3 prev = p1;
            for (int s = 1; s <= segs; s++)
            {
                float t = s / (float)segs;
                Vector3 next = CatmullRomPoint(p0, p1, p2, p3, t);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }

    static Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
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