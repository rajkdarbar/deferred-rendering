using UnityEngine;

[DefaultExecutionOrder(100)]
public class LightingPass : MonoBehaviour
{
    public GBufferSetup gBuffer;
    public Material lightingMat;
    public Light mainLight;
    public Camera mainCam;

    [HideInInspector] public RenderTexture lightingRT;

    void OnEnable()
    {
        var cam = GetComponent<Camera>() ?? Camera.main;
        if (cam) cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (gBuffer == null || lightingMat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        if (mainLight == null) mainLight = FindObjectOfType<Light>();
        if (mainCam == null) mainCam = Camera.main;

        // Ensure lighting RT matches screen size
        if (lightingRT == null || lightingRT.width != src.width || lightingRT.height != src.height)
        {
            if (lightingRT != null) lightingRT.Release();

            lightingRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                name = "LightingRT",
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
            };
            lightingRT.enableRandomWrite = false;
            lightingRT.Create();
        }

        // Pass GBuffer data
        lightingMat.SetTexture("_AlbedoRT", gBuffer.albedoRT);
        lightingMat.SetTexture("_NormalRT", gBuffer.normalRT);
        lightingMat.SetTexture("_SpecRT", gBuffer.specRT);

        // Pass directional light info (converted to view-space)
        Matrix4x4 worldToView = mainCam.worldToCameraMatrix;

        // Directional light direction is *from* surface to light, so negate forward
        Vector3 lightDirWS = -mainLight.transform.forward;
        Vector3 lightDirVS = worldToView.MultiplyVector(lightDirWS);

        lightingMat.SetVector("_LightDir", lightDirVS);
        lightingMat.SetVector("_LightColor", mainLight.color * mainLight.intensity);

        // In view-space, the camera always looks down +Z axis
        lightingMat.SetVector("_ViewDir", new Vector3(0, 0, 1));


        // Render lighting into persistent RT
        Graphics.Blit(null, lightingRT, lightingMat);

        // Output to screen
        Graphics.Blit(lightingRT, dest);
    }

    void OnDisable()
    {
        if (lightingRT != null)
        {
            lightingRT.Release();
            lightingRT = null;
        }
    }
}
