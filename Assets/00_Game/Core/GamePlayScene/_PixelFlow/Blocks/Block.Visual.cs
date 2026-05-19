using UnityEngine;

public partial class Block
{
    public Renderer body;

    private static readonly int ColorID = Shader.PropertyToID("_Color");        // Built-in / Standard
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP / HDRP / Lit
    private static MaterialPropertyBlock _mpb;

    public void SetColor(Color c)
    {
        if (body == null) return;

        // SpriteRenderer dùng .color trực tiếp (MaterialPropertyBlock không work với sprite)
        if (body is SpriteRenderer sr)
        {
            sr.color = c;
            return;
        }

        // Mesh / Skinned / etc → dùng MaterialPropertyBlock để không sinh material instance
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        body.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorID, c);     // shader Built-in
        _mpb.SetColor(BaseColorID, c); // shader URP/HDRP
        body.SetPropertyBlock(_mpb);
    }
}