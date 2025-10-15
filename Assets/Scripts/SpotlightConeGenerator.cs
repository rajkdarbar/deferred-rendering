using UnityEngine;
using System.Collections.Generic;

public class SpotlightConeGenerator : MonoBehaviour
{
    public Light sourceLight; // assign a spotlight    
    public Mesh coneMesh; // generated cone mesh (apex at origin, +Z is forward, height=1, base radius=1)

    [Range(8, 128)] public int segments = 24;

    void OnEnable()
    {
        EnsureMesh();
        SyncToLight();
        ApplyRendererState();
    }

    void Update()
    {
        SyncToLight();
        ApplyRendererState();
    }

    void OnValidate()
    {
        EnsureMesh(); // rebuild when segments change in inspector
        SyncToLight();
        ApplyRendererState();
    }

    void EnsureMesh()
    {
        if (!coneMesh) coneMesh = CreateUnitCone(segments);

        var mf = GetComponent<MeshFilter>();
        if (!mf) mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = coneMesh;

        var meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
    }

    void SyncToLight()
    {
        if (!sourceLight || sourceLight.type != LightType.Spot) return;

        // Place & aim exactly like the spotlight
        transform.position = sourceLight.transform.position;
        transform.rotation = sourceLight.transform.rotation;

        // Scale: unit cone (apex at 0, base at z=1) -> scale XY by radius, Z by range
        float range = sourceLight.range;
        float radius = Mathf.Tan(sourceLight.spotAngle * Mathf.Deg2Rad * 0.5f) * range;
        transform.localScale = new Vector3(radius, radius, range);
    }

    void ApplyRendererState()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    // Unit cone: apex at (0,0,0), base at z=1, radius=1, forward = +Z
    Mesh CreateUnitCone(int seg)
    {
        seg = Mathf.Max(3, seg);
        var m = new Mesh { name = "UnitSpotCone" };
        var v = new List<Vector3>(seg + 3);
        var tris = new List<int>(seg * 6);

        v.Add(Vector3.zero); // 0 = apex
        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 1f)); // base ring at z=1
        }

        int baseCenterIndex = v.Count;
        v.Add(new Vector3(0, 0, 1f));

        // Side faces (winding for outside when looking from outside the cone)
        for (int i = 1; i <= seg; i++)
        {
            tris.Add(0); // apex
            tris.Add(i + 1);
            tris.Add(i);
        }

        // Base cap for solid volume
        for (int i = 1; i <= seg; i++)
        {
            tris.Add(baseCenterIndex);
            tris.Add(i);
            tris.Add(i + 1);
        }

        m.SetVertices(v);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}