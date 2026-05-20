using System.Collections.Generic;
using UnityEngine;

public class WaitAreaController : StaffSingleton<WaitAreaController>
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

    public WaitArea FindEmptyWaitArea()
    {
        foreach (var wa in waitAreas)
            if (wa != null && wa.IsEmpty) return wa;
        return null;
    }
    public bool AreAllFull() => !waitAreas[waitAreas.Count - 1].IsEmpty;

    public void  PlayAllWarningBlink()
    {
        foreach (var wa in waitAreas)
            wa.PlayWarningBlink();
    }
    public void ShiftForward()
    {
        int writeIdx = 0;

        for (int readIdx = 0; readIdx < waitAreas.Count; readIdx++)
        {
            WaitArea readArea = waitAreas[readIdx];
            if (readArea.Occupant == null) continue;

            Shooter shooter = readArea.Occupant;

            if (readIdx == writeIdx)
            {
                writeIdx++;
                continue;
            }

            WaitArea writeArea = waitAreas[writeIdx];
            readArea.ResetToDefault();
            writeArea.AddOccupant(shooter);
            shooter.transform.SetParent(writeArea.transform);
            shooter.JumpTo(writeArea.transform.position);
            writeIdx++;
        }
    }
    public override void Init()
    {

    }
}