using TMPro;
using UnityEngine;

public partial class Shooter
{
    [Space(10), Header("Visual")]
    public MeshRenderer body;
    public GameObject bodyVisual;
    public TextMeshPro txtBody;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;
    private Color _myColor;

    public Color BodyColor => _myColor;

    public void SetColor(Color color)
    {
        _myColor = color;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        body.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, color);
        body.SetPropertyBlock(_mpb);
    }

    public void SetBodyVisible(bool visible)
    {
        bodyVisual.SetActive(visible);
    }
}
