using EventDispatcher;
using TMPro;
using UnityEngine;

public class IceProp : PropHandler
{
    [SerializeField] private GameObject iceVisual;
    [SerializeField] private TMP_Text iceTxt;
    [SerializeField] private ParticleSystem iceBreakFx;
    [SerializeField, Range(0.05f, 0.5f)] private float iceChipScale = 0.2f;
    [SerializeField, Min(1f)] private float iceChipSpread = 4f;
    [SerializeField] private int iceCount;

    public override PropState Key => PropState.Ice;

    public int Count => iceCount;

    public void SetCount(int count)
    {
        iceCount = count;
        if (iceTxt != null) iceTxt.text = count.ToString();
    }

    public override void OnAttach(Shooter owner)
    {
        base.OnAttach(owner);
        if (iceVisual != null) iceVisual.SetActive(true);
        owner.SetBodyVisible(false);
        this.RegisterListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
    }

    public override void OnDetach()
    {
        this.RemoveListener(EventID.BLOCK_DESTROYED, OnBlockDestroyed);
        if (iceVisual != null) iceVisual.SetActive(false);
        Owner?.SetBodyVisible(true);
        base.OnDetach();
    }

    private void OnBlockDestroyed(object param)
    {
        if (iceCount <= 0 || Owner == null) return;
        iceCount--;
        if (iceTxt != null) iceTxt.text = iceCount.ToString();

        PlayIceFeedback(iceCount <= 0);

        if (iceCount <= 0)
            Owner.RemoveProps(PropState.Ice);
    }

    private void PlayIceFeedback(bool broken)
    {
        if (iceBreakFx == null) return;

        // Keep the burst alive after the ice model is hidden on the final hit.
        ParticleSystem burst = Instantiate(iceBreakFx, iceBreakFx.transform.position,
            iceBreakFx.transform.rotation);
        burst.transform.localScale = iceBreakFx.transform.lossyScale * (broken ? 1f : iceChipScale);
        burst.gameObject.SetActive(true);
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float duration = 0f;
        foreach (ParticleSystem particles in burst.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            duration = Mathf.Max(duration, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);

            if (broken) continue;

            main.startSpeedMultiplier *= iceChipSpread;
            var shape = particles.shape;
            shape.radius *= iceChipSpread;

            // A short, sparse burst of tiny ice chips for each remaining layer.
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)5, (short)8) });
        }

        burst.Play(true);
        Destroy(burst.gameObject, duration + 0.5f);
    }
}
