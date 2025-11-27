using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Ensures a scene-level post-processing volume is configured and the main camera
/// has a PostProcessLayer ready for bloom/vignette.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(PostProcessVolume))]
public class NeonPostProcessBootstrap : MonoBehaviour
{
    [SerializeField] PostProcessProfile profile;
    [SerializeField] Camera targetCamera;
    [SerializeField] LayerMask volumeLayer = ~0;

    PostProcessVolume m_Volume;

    void OnEnable()
    {
        Setup();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            Setup();
        }
    }
#endif

    void Setup()
    {
        if (profile == null)
        {
            return;
        }

        if (m_Volume == null)
        {
            m_Volume = GetComponent<PostProcessVolume>();
        }

        if (m_Volume == null)
        {
            return;
        }

        m_Volume.isGlobal = true;
        m_Volume.sharedProfile = profile;
        m_Volume.priority = 25f;
        m_Volume.weight = 1f;
        m_Volume.blendDistance = 0.1f;

        var cameraToSetup = targetCamera != null ? targetCamera : GetComponentInChildren<Camera>();
        if (cameraToSetup == null)
        {
            return;
        }

        var layer = cameraToSetup.GetComponent<PostProcessLayer>();
        if (layer == null)
        {
            layer = cameraToSetup.gameObject.AddComponent<PostProcessLayer>();
        }

        layer.volumeLayer = volumeLayer;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        layer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
        layer.fastApproximateAntialiasing.fastMode = false;
        layer.fastApproximateAntialiasing.keepAlpha = true;
    }
}
