using UnityEngine;

public abstract class PropHandler : MonoBehaviour
{
    public abstract PropState Key { get; }

    protected Shooter Owner { get; private set; }

    public virtual void OnAttach(Shooter owner) { Owner = owner; }
    public virtual void OnDetach() { Owner = null; }
}
