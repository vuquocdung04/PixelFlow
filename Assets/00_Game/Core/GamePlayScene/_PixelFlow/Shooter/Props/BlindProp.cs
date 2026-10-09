using UnityEngine;
using DG.Tweening;

public class BlindProp : PropHandler
{
    [SerializeField] private GameObject blindVisual;

    public override PropState Key => PropState.Blind;

    [SerializeField, Min(1)] private int revealParticleCount = 8;
    [SerializeField, Min(0f)] private float revealParticleLifetime = 0.45f;

    public override void OnAttach(Shooter owner)
    {
        base.OnAttach(owner);
        if (blindVisual != null) blindVisual.SetActive(true);
        owner.SetBodyVisible(false);
    }

    public override void OnDetach()
    {
        PlayRevealBurst();
        if (blindVisual != null) blindVisual.SetActive(false);
        Owner?.SetBodyVisible(true);
        base.OnDetach();
    }

    private void PlayRevealBurst()
    {
        if (blindVisual == null) return;

        SpriteRenderer source = blindVisual.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null) return;

        Vector3 origin = source.transform.position;
        float lifetime = Mathf.Max(0.1f, revealParticleLifetime);

        for (int i = 0; i < revealParticleCount; i++)
        {
            GameObject piece = new GameObject("Hidden Reveal ?");
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + 1;
            piece.transform.position = origin;
            piece.transform.rotation = source.transform.rotation;
            piece.transform.localScale = source.transform.lossyScale * Random.Range(0.35f, 0.7f);

            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
            Vector3 target = origin + new Vector3(direction.x, direction.y, Random.Range(-0.15f, 0.15f)) * Random.Range(0.35f, 0.75f);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(piece.transform.DOMove(target, lifetime).SetEase(Ease.OutCubic));
            sequence.Join(piece.transform.DORotate(new Vector3(0f, 0f, Random.Range(-180f, 180f)), lifetime, RotateMode.FastBeyond360));
            sequence.Join(renderer.DOFade(0f, lifetime * 0.75f).SetDelay(lifetime * 0.25f));
            sequence.OnComplete(() => Destroy(piece));
        }
    }
}
