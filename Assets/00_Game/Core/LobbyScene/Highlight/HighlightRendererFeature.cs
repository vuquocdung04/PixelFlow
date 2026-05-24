using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HighlightRendererFeature : ScriptableRendererFeature
{
  [System.Serializable]
  public class FeatureSettings
  {
    public Material overlayMaterial;
    public LayerMask highlightLayerMask;
  }

  public FeatureSettings settings = new();
  DarkOverlayPass _darkPass;
  HighlightObjectsPass _highlightPass;

  public static bool IsActive { get; set; }
  
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void ResetStaticState()
  {
    IsActive = false;
  }
  public override void Create()
  {
    _darkPass = new DarkOverlayPass(settings.overlayMaterial)
    {
      renderPassEvent = RenderPassEvent.AfterRenderingTransparents
    };
    _highlightPass = new HighlightObjectsPass(settings.highlightLayerMask)
    {
      renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1
    };
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    if (!IsActive || settings.overlayMaterial == null) return;
    if (renderingData.cameraData.renderType == CameraRenderType.Overlay) return;
    renderer.EnqueuePass(_darkPass);
    renderer.EnqueuePass(_highlightPass);
  }

  class DarkOverlayPass : ScriptableRenderPass
  {
    readonly Material _mat;
    class PassData { public Material material; }
    public DarkOverlayPass(Material mat) => _mat = mat;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
      if (_mat == null) return;
      var resourceData = frameData.Get<UniversalResourceData>();

      using var builder = renderGraph.AddRasterRenderPass<PassData>("DarkOverlay", out var passData);
      passData.material = _mat;

      builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
      builder.AllowPassCulling(false);
      builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
      {
        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
      });
    }
  }

  class HighlightObjectsPass : ScriptableRenderPass
  {
    readonly LayerMask _layerMask;
    static readonly List<ShaderTagId> ShaderTags = new() { new("UniversalForward"), new("LightweightForward"), new("SRPDefaultUnlit") };
    class PassData { public RendererListHandle rendererList; }
    public HighlightObjectsPass(LayerMask layerMask) => _layerMask = layerMask;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
      var renderingData = frameData.Get<UniversalRenderingData>();
      var cameraData = frameData.Get<UniversalCameraData>();
      var lightData = frameData.Get<UniversalLightData>();
      var resourceData = frameData.Get<UniversalResourceData>();

      var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
      var filterSettings = new FilteringSettings(RenderQueueRange.all, _layerMask);
      var listParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

      using var builder = renderGraph.AddRasterRenderPass<PassData>("HighlightObjects", out var passData);
      passData.rendererList = renderGraph.CreateRendererList(listParams);

      builder.UseRendererList(passData.rendererList);
      builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
      builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
      builder.AllowPassCulling(false);
      builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
      {
        ctx.cmd.DrawRendererList(data.rendererList);
      });
    }
  }
}