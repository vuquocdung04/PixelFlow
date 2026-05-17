using System.Collections.Generic;
using UnityEngine;

public class WaitAreaController : MonoBehaviour
{
    public int initialItemCount = 4;
    public float spacing;
    public float startZ;
    public WaitArea waitArea;

    public List<WaitArea> waitAreas = new List<WaitArea>();

    [ContextMenu("Spawn Wait Areas")]
    public void SpawnWaitAreas()
    {
        ClearWaitAreas();

        float offset = (initialItemCount - 1) * spacing * 0.5f;

        for (int i = 0; i < initialItemCount; i++)
        {
            WaitArea instance = Instantiate(waitArea, transform);
            instance.transform.position = new Vector3(
                transform.position.x - offset + i * spacing,
                transform.position.y,
                startZ
            );
            waitAreas.Add(instance);
        }
    }

    void ClearWaitAreas()
    {
        for (int i = waitAreas.Count - 1; i >= 0; i--)
        {
            if (waitAreas[i] == null) continue;
            if (Application.isPlaying)
                Destroy(waitAreas[i].gameObject);
            else
                DestroyImmediate(waitAreas[i].gameObject);
        }
        waitAreas.Clear();
    }
}