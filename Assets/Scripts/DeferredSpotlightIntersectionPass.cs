using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(200)]
public class DeferredSpotlightIntersectionPass : MonoBehaviour
{
    public Material spotlightIntersectionMat;
    public GBufferSetup gBuffer;
    public Light[] spotLights;

    public bool debugMask = false;
    [Range(0, 4)] public int debugMaskIndex = 0;

    [HideInInspector] public List<RenderTexture> maskRTs = new List<RenderTexture>();

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>() ?? Camera.main;
    }

    void OnDisable()
    {
        foreach (var rt in maskRTs)
        {
            if (rt)
            {
                rt.Release();
            }
        }

        maskRTs.Clear();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!spotlightIntersectionMat || !cam)
        {
            Graphics.Blit(src, dest);
            return;
        }

        int lightCount = spotLights != null ? spotLights.Length : 0;
        if (lightCount == 0)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // Ensure enough RTs for all lights
        while (maskRTs.Count < lightCount)
        {
            var rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.R8);
            rt.Create();
            rt.filterMode = FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Clamp;
            maskRTs.Add(rt);
        }

        spotlightIntersectionMat.SetFloat("_Epsilon", 0.001f);
        spotlightIntersectionMat.SetTexture("_ViewZRT", gBuffer.viewZRT);
        spotlightIntersectionMat.SetMatrix("_CameraInvProjection", cam.projectionMatrix.inverse);

        // Manually rendering per-spotlight cone for mask generation
        var viewMatrix = cam.worldToCameraMatrix;
        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = viewMatrix;

        for (int i = 0; i < lightCount; i++)
        {
            var L = spotLights[i];
            if (!L || L.type != LightType.Spot) continue;

            var spotCone = L.GetComponentInChildren<SpotlightConeGenerator>();
            if (!spotCone) continue;

            var mesh = spotCone.coneMesh ?? spotCone.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;

            // Clear mask for this light
            Graphics.SetRenderTarget(maskRTs[i]);
            GL.Clear(true, true, Color.black);

            // Draw intersection
            spotlightIntersectionMat.SetMatrix("_ConeViewMatrix", viewMatrix * spotCone.transform.localToWorldMatrix);
            spotlightIntersectionMat.SetPass(0);
            Graphics.DrawMeshNow(mesh, spotCone.transform.localToWorldMatrix);
        }

        GL.PopMatrix();

        // Debug maskRT visualization
        if (debugMask && maskRTs.Count > 0)
        {
            int idx = Mathf.Clamp(debugMaskIndex, 0, maskRTs.Count - 1);
            Graphics.Blit(maskRTs[idx], dest);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
