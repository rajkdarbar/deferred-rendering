using UnityEngine;

public class GBufferDebugView : MonoBehaviour
{
    public GBufferSetup gBuffer;

    void OnGUI()
    {
        if (gBuffer == null) return;

        int cols = 3;
        int rows = 2;
        int w = Screen.width / cols;
        int h = Screen.height / rows;

        int index = 0;

        void DrawRT(RenderTexture tex, string label)
        {
            if (tex == null) return;
            int col = index % cols;
            int row = index / cols;
            Rect rect = new Rect(col * w, row * h, w, h);
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
            GUI.Label(new Rect(rect.x + 10, rect.y + 10, 200, 20), label);
            index++;
        }

        // Draw all six GBuffer textures
        DrawRT(gBuffer.albedoRT, "Albedo");
        DrawRT(gBuffer.normalRT, "Normal");
        DrawRT(gBuffer.specRT, "Specular");
        DrawRT(gBuffer.depthRT, "Depth");
        DrawRT(gBuffer.viewPosRT, "View Position");
        DrawRT(gBuffer.viewZRT, "View Z");
    }
}
