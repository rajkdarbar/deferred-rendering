using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)]
public class GBufferSetup : MonoBehaviour
{
    public Camera mainCam;

    [Header("GBuffer Render Targets")]
    public RenderTexture albedoRT;
    public RenderTexture normalRT;
    public RenderTexture specRT;
    public RenderTexture depthRT; // hardware depth
    public RenderTexture viewPosRT; // full view-space XYZ
    public RenderTexture viewZRT; // only view-space Z

    private RenderTargetIdentifier[] mrt;
    private CommandBuffer cmd;
    private int currentWidth, currentHeight;

    void Start()
    {
        mainCam ??= Camera.main;

        EnsureRenderTargets();
        SetupCommandBuffer();
    }

    void Update()
    {
        if (ScreenResized())
            CreateGBufferTargets(Screen.width, Screen.height);

        if (cmd == null) return;
        RenderSceneToGBuffer();
    }

    void OnDisable()
    {
        ReleaseRenderTextures();

        if (mainCam && cmd != null)
            mainCam.RemoveCommandBuffer(CameraEvent.AfterEverything, cmd);
    }

    void EnsureRenderTargets()
    {
        if (albedoRT && normalRT && specRT && depthRT && viewPosRT && viewZRT) return;
        CreateGBufferTargets(Screen.width, Screen.height);
    }

    void SetupCommandBuffer()
    {
        if (cmd != null) return;
        cmd = new CommandBuffer { name = "Custom GBuffer" };
        mainCam.AddCommandBuffer(CameraEvent.AfterEverything, cmd);
    }

    bool ScreenResized()
    {
        return Screen.width != currentWidth || Screen.height != currentHeight;
    }


    void RenderSceneToGBuffer()
    {
        cmd.Clear();
        cmd.SetRenderTarget(mrt, new RenderTargetIdentifier(depthRT));
        cmd.ClearRenderTarget(true, true, Color.black);

        foreach (var rend in FindObjectsOfType<MeshRenderer>())
        {
            if (!TryGetMesh(rend, out Mesh mesh)) continue;
            var mat = rend.sharedMaterial;
            if (mat && mat.shader.name == "Custom/GBuffer")
            {
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    cmd.DrawMesh(mesh, rend.localToWorldMatrix, mat, sub, 0);
            }
        }
    }

    bool TryGetMesh(MeshRenderer rend, out Mesh mesh)
    {
        mesh = rend.GetComponent<MeshFilter>()?.sharedMesh;
        return mesh != null;
    }

    void CreateGBufferTargets(int w, int h)
    {
        ReleaseRenderTextures();

        albedoRT = CreateRT(w, h, 0, RenderTextureFormat.ARGB32, "AlbedoRT");
        normalRT = CreateRT(w, h, 0, RenderTextureFormat.ARGBHalf, "NormalRT");
        specRT = CreateRT(w, h, 0, RenderTextureFormat.ARGB32, "SpecRT");

        viewPosRT = CreateRT(w, h, 0, RenderTextureFormat.ARGBHalf, "ViewPosRT", FilterMode.Point);
        viewZRT = CreateRT(w, h, 0, RenderTextureFormat.ARGBHalf, "ViewZRT", FilterMode.Point);

        depthRT = CreateRT(w, h, 24, RenderTextureFormat.Depth, "DepthRT", FilterMode.Point);

        mrt = new[]
        {
            new RenderTargetIdentifier(albedoRT),
            new RenderTargetIdentifier(normalRT),
            new RenderTargetIdentifier(specRT),
            new RenderTargetIdentifier(viewPosRT),
            new RenderTargetIdentifier(viewZRT)
        };

        currentWidth = w;
        currentHeight = h;

        //Debug.Log($"[GBufferSetup] Created MRTs: {w}x{h}");
    }

    RenderTexture CreateRT(int w, int h, int depth, RenderTextureFormat format, string name, FilterMode filter = FilterMode.Bilinear)
    {
        var rt = new RenderTexture(w, h, depth, format)
        {
            name = name,
            filterMode = filter
        };
        rt.Create();
        return rt;
    }

    void ReleaseRenderTextures()
    {
        ReleaseRT(albedoRT);
        ReleaseRT(normalRT);
        ReleaseRT(specRT);
        ReleaseRT(depthRT);
        ReleaseRT(viewPosRT);
        ReleaseRT(viewZRT);
    }

    void ReleaseRT(RenderTexture rt)
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}