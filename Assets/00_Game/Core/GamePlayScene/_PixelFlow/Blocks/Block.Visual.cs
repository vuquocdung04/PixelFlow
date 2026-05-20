using UnityEngine;

public partial class Block
{
    public MeshRenderer body;
    public MeshRenderer bodyBot;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock _mpb;

    public void SetColor(Color c)
    {
        ApplyColorToRenderer(body, c);
        ApplyColorToRenderer(bodyBot, c);
    }

    private void ApplyColorToRenderer(Renderer targetRenderer, Color c)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, c);
        targetRenderer.SetPropertyBlock(_mpb);
    }
}