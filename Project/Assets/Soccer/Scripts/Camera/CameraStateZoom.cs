using UnityEngine;

/// <summary>
/// Listens for UI screen changes and adjusts the camera follow offset accordingly.
/// </summary>
public class CameraStateZoom : MonoBehaviour
{
    [Tooltip("Target camera controller that will be manipulated.")]
    public CameraController cameraController;

    [Header("Offsets")]
    public Vector3 lobbyFollowOffset = new Vector3(0f, 100f, -50f);
    public Vector3 gameplayFollowOffset = new Vector3(0f, 30f, -40f);

    [Tooltip("If enabled, the lobby offset is applied immediately on Awake.")]
    public bool snapLobbyOffsetOnAwake = true;

    void Awake()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<CameraController>();
        }

        if (cameraController != null && snapLobbyOffsetOnAwake)
        {
            cameraController.SetFollowOffset(lobbyFollowOffset, true);
        }
    }

    void OnEnable()
    {
        UniformUIScreenManager.OnScreenStateChanged += HandleScreenChanged;
    }

    void OnDisable()
    {
        UniformUIScreenManager.OnScreenStateChanged -= HandleScreenChanged;
    }

    void HandleScreenChanged(UniformUIScreenManager.ScreenState state)
    {
        if (cameraController == null)
        {
            return;
        }

        if (state == UniformUIScreenManager.ScreenState.Game)
        {
            cameraController.SetFollowOffset(gameplayFollowOffset);
        }
        else
        {
            cameraController.SetFollowOffset(lobbyFollowOffset);
        }
    }
}
