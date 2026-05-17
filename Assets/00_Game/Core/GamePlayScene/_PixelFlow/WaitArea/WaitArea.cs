using UnityEngine;

public class WaitArea : MonoBehaviour
{
    [field: SerializeField] public Shooter Occupant { get; private set; }
    public bool IsEmpty => Occupant == null;
    public void AddOccupant(Shooter shooter)
    {
        Occupant = shooter;
    }
    public void ResetToDefault()
    {
        Occupant = null;
    }
}