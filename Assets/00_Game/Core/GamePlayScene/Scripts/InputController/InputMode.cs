using UnityEngine;

public abstract class InputMode
{
    protected InputController Controller { get; private set; }

    public virtual void OnEnter(InputController controller)
    {
        Controller = controller;
    }

    public virtual void OnExit()
    {
    }

    public abstract void HandleClick(RaycastHit hit);
}