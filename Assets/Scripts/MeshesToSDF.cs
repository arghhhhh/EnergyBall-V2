using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.SDF;
using UnityEngine.VFX.Utility;

public class MeshesToSDF : MonoBehaviour
{
    SceneController controller = null;

    private class MeshData
    {
        public Mesh mesh;
        public Matrix4x4 matrix;
    }

    MeshToSDFBaker sdfBaker;
    Mesh mesh;
    readonly Dictionary<ulong, MeshData> meshesData = new();

    [Header("Box")]
    public bool setBoxBoundsAutomatically = false;

    [DisableIf("setBoxBoundsAutomatically")]
    public Vector3 center;

    [DisableIf("setBoxBoundsAutomatically")]
    public Vector3 sizeBox = Vector3.one;
    public int maxResolution = 32;

    [Header("SDF Baker")]
    [Range(1, 16)]
    public int signPassesCount = 1;

    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Range(-1f, 1f)]
    public float offset = 0f;

    [Header("VFX Graph")]
    [SerializeField]
    private ExposedProperty sdfTextureProperty = "sdfTexture";

    [SerializeField]
    private ExposedProperty sdfPositionProperty = "sdfPosition";

    [SerializeField]
    private ExposedProperty sdfScaleProperty = "sdfScale";

    public Vector3 CenterWS => transform.TransformPoint(center); // center in world space

    private void OnValidate()
    {
        sizeBox = new Vector3(
            Mathf.Max(0, sizeBox.x),
            Mathf.Max(0, sizeBox.y),
            Mathf.Max(0, sizeBox.z)
        );
    }

    void Start()
    {
        controller = SceneController.Instance;
    }

    void Update()
    {
        if (controller.Players.Count > 0)
        {
            CombineMeshes();

            if (sdfBaker == null)
            {
                sdfBaker = new MeshToSDFBaker(
                    sizeBox,
                    CenterWS,
                    maxResolution,
                    mesh,
                    signPassesCount,
                    threshold,
                    offset
                );
            }
            else
            {
                sdfBaker.Reinit(
                    sizeBox,
                    CenterWS,
                    maxResolution,
                    mesh,
                    signPassesCount,
                    threshold,
                    offset
                );
            }

            // Automatically set box bounds
            if (setBoxBoundsAutomatically)
            {
                center = mesh.bounds.center;
                sizeBox = mesh.bounds.size;
            }

            sdfBaker.BakeSDF();

            VisualEffect[] playerVfx = FindObjectsOfType<VisualEffect>();

            foreach (VisualEffect vfx in playerVfx)
            {
                vfx.SetTexture(sdfTextureProperty, sdfBaker.SdfTexture);
                vfx.SetVector3(sdfScaleProperty, sizeBox);
                vfx.SetVector3(sdfPositionProperty, CenterWS);
            }
        }
    }

    public void AddToMeshes(PlayerConstructor player)
    {
        Mesh playerMesh = player.sphere.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer meshRenderer = player.sphere.GetComponent<MeshRenderer>();
        MeshData meshData = new() { mesh = playerMesh, matrix = meshRenderer.localToWorldMatrix };
        meshesData[player.userId] = meshData;

        // Debug.Log("Mesh added to meshes");
    }

    public void RemoveMeshFromMeshes(PlayerConstructor player)
    {
        meshesData.Remove(player.userId);
    }

    public void UpdateMeshTransform(PlayerConstructor player)
    {
        if (meshesData.ContainsKey(player.userId))
        {
            meshesData[player.userId].mesh = player.sphere.GetComponent<MeshFilter>().sharedMesh;
            meshesData[player.userId].matrix = player
                .sphere.GetComponent<MeshRenderer>()
                .localToWorldMatrix;
        }
    }

    void CombineMeshes()
    {
        CombineInstance[] combine = new CombineInstance[meshesData.Count];
        // Debug.Log("# of meshes: " + meshes.Count);

        int i = 0;
        while (i < meshesData.Count)
        {
            combine[i].mesh = meshesData.ElementAt(i).Value.mesh;
            combine[i].transform = meshesData.ElementAt(i).Value.matrix;
            i++;
        }

        mesh = new Mesh();
        mesh.CombineMeshes(combine, true, true);
    }

    private void OnDestroy()
    {
        sdfBaker?.Dispose();
        sdfBaker = null;
    }

    private void OnDrawGizmosSelected()
    {
        // Baking box gizmo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(CenterWS, sizeBox);
    }
}
