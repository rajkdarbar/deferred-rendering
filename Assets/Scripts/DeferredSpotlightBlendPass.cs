using UnityEngine;

[DefaultExecutionOrder(210)]
public class DeferredSpotlightBlendPass : MonoBehaviour
{
    public Material spotlightBlendMat;
    public GBufferSetup gBuffer;
    public DeferredSpotlightIntersectionPass intersectionPass; // provides maskRTs[]
    public LightingPass lightingPass; // base lighting
    public Light[] spotLights;

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>() ?? Camera.main;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!spotlightBlendMat || gBuffer == null || intersectionPass == null ||
            lightingPass == null || spotLights == null || spotLights.Length == 0)
        {
            Graphics.Blit(src, dest);
            return;
        }

        RenderTexture baseLighting = lightingPass.lightingRT;
        if (baseLighting == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // Initialize final target
        RenderTexture tempRT1 = RenderTexture.GetTemporary(baseLighting.descriptor);
        Graphics.Blit(baseLighting, tempRT1); // start from base lighting

        Matrix4x4 worldToView = cam.worldToCameraMatrix;

        // Loop through each spotlight
        int count = Mathf.Min(spotLights.Length, intersectionPass.maskRTs.Count);
        for (int i = 0; i < count; i++)
        {
            var L = spotLights[i];
            if (!L || !L.enabled || !L.gameObject.activeInHierarchy) continue;

            var mask = intersectionPass.maskRTs[i];
            if (!mask) continue;
            spotlightBlendMat.SetTexture("_SpotMask", mask);

            spotlightBlendMat.SetTexture("_MainTex", tempRT1);
            spotlightBlendMat.SetTexture("_NormalRT", gBuffer.normalRT);
            spotlightBlendMat.SetTexture("_AlbedoRT", gBuffer.albedoRT);
            spotlightBlendMat.SetTexture("_SpecularRT", gBuffer.specRT);
            spotlightBlendMat.SetTexture("_ViewZRT", gBuffer.viewZRT);
            spotlightBlendMat.SetMatrix("_CameraInvProjection", cam.projectionMatrix.inverse);

            Vector3 lightPosVS = worldToView.MultiplyPoint(L.transform.position);
            Vector3 lightDirVS = worldToView.MultiplyVector(L.transform.forward);

            spotlightBlendMat.SetVector("_LightPosVS", new Vector4(lightPosVS.x, lightPosVS.y, lightPosVS.z, 1));
            spotlightBlendMat.SetVector("_LightDirVS", new Vector4(lightDirVS.x, lightDirVS.y, lightDirVS.z, 0));
            spotlightBlendMat.SetColor("_LightColor", L.color * L.intensity);
            spotlightBlendMat.SetFloat("_LightRange", L.range);
            spotlightBlendMat.SetFloat("_SpotAngleCos", Mathf.Cos(L.spotAngle * 0.5f * Mathf.Deg2Rad));

            // Render this spotlight over temp -> dest, then swap
            RenderTexture tempRT2 = RenderTexture.GetTemporary(baseLighting.descriptor);
            Graphics.Blit(tempRT1, tempRT2, spotlightBlendMat);
            RenderTexture.ReleaseTemporary(tempRT1);
            tempRT1 = tempRT2;
        }

        Graphics.Blit(tempRT1, dest);
        RenderTexture.ReleaseTemporary(tempRT1);
    }
}
