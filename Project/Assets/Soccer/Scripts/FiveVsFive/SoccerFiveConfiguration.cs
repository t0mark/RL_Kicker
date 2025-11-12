using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SoccerEnvController))]
public class SoccerFiveConfiguration : MonoBehaviour
{
    [Header("Field Size")]
    [SerializeField]
    Vector2 fieldHalfSize = new Vector2(28f, 18f);

    [Header("Spawn Settings")]
    [SerializeField]
    Vector2 playerSpawnJitter = new Vector2(9f, 6f);

    [SerializeField]
    Vector2 ballSpawnJitter = new Vector2(6f, 6f);

    [Header("Defensive Shape")]
    [SerializeField]
    float defensiveShellRadius = 15f;

    [SerializeField]
    float defensiveMaxRadius = 22f;

    SoccerEnvController m_Controller;

    void Awake()
    {
        m_Controller = GetComponent<SoccerEnvController>();
        ApplyConfiguration();
    }

    public void ApplyConfiguration()
    {
        if (m_Controller == null)
        {
            return;
        }

        m_Controller.SetFieldHalfSize(fieldHalfSize);
        m_Controller.SetPlayerSpawnJitter(playerSpawnJitter);
        m_Controller.SetBallSpawnJitter(ballSpawnJitter);
        m_Controller.SetDefensiveRadii(defensiveShellRadius, defensiveMaxRadius);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (m_Controller == null)
        {
            m_Controller = GetComponent<SoccerEnvController>();
        }
        if (!Application.isPlaying)
        {
            ApplyConfiguration();
        }
    }
#endif
}
