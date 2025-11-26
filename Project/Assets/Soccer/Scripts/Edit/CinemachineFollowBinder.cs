using UnityEngine;
using Unity.Cinemachine;

public class CinemachineFollowBinder : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineCamera vcam;
    public PlayerSwitchManager switchManager;

    void Awake()
    {
        if (!vcam)
            vcam = FindFirstObjectByType<CinemachineCamera>();

        if (!switchManager)
            switchManager = FindFirstObjectByType<PlayerSwitchManager>();
    }

    void OnEnable()
    {
        PlayerSwitchManager.OnControlledChanged += HandleControlledChanged;

        if (switchManager != null && switchManager.CurrentControlled != null)
        {
            HandleControlledChanged(switchManager.CurrentControlled);
        }
    }

    void OnDisable()
    {
        PlayerSwitchManager.OnControlledChanged -= HandleControlledChanged;
    }

    void HandleControlledChanged(Transform t)
    {
        if (!vcam) return;

        vcam.Follow = t;
        vcam.LookAt = t;
    }
}
