using UnityEngine;

public class Rope3D : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform rope;            // cylinder, pivot ở bottom (đầu Z=0)
    public float thickness = 0.1f;
    public float ropeNativeLength = 1f; // chiều dài cylinder khi scaleZ = 1

    void LateUpdate()
    {
        Vector3 dir = pointB.position - pointA.position;
        float dist = dir.magnitude;

        rope.position = pointA.position;

        if (dist > 0.0001f)
            rope.forward = dir / dist;   // align trục +Z với hướng A→B

        rope.localScale = new Vector3(thickness, thickness, dist / ropeNativeLength);
    }
}