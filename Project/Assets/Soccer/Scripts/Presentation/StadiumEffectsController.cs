using System.Collections;
using UnityEngine;

/// <summary>
/// Bridges gameplay events to stadium particle/light effects.
/// </summary>
[ExecuteAlways]
public class StadiumEffectsController : MonoBehaviour
{
    [Header("Goal Lighting")]
    [SerializeField] Light[] blueGoalLights;
    [SerializeField] Light[] purpleGoalLights;
    [SerializeField] float goalPulseDuration = 1.5f;
    [SerializeField] AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Particle Styling")]
    [SerializeField] Color kickSparkColor = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] Color goalBurstColor = new Color(0.78f, 0.32f, 0.95f, 1f);
    [SerializeField] float kickSparkSize = 0.5f;
    [SerializeField] float goalBurstSize = 3f;

    ParticleSystem m_KickSystem;
    ParticleSystem m_GoalSystem;
    Coroutine m_GoalPulseRoutine;

    void Awake()
    {
        EnsureSystems();
    }

    void OnEnable()
    {
        EnsureSystems();
        StadiumEffectEvents.BallKicked += OnBallKicked;
        StadiumEffectEvents.GoalScored += OnGoalScored;
    }

    void OnDisable()
    {
        StadiumEffectEvents.BallKicked -= OnBallKicked;
        StadiumEffectEvents.GoalScored -= OnGoalScored;
    }

    void EnsureSystems()
    {
        if (m_KickSystem == null)
        {
            m_KickSystem = CreateSystem("KickSparkSystem", kickSparkSize, 0.15f, 25, kickSparkColor);
        }

        if (m_GoalSystem == null)
        {
            m_GoalSystem = CreateSystem("GoalBurstSystem", goalBurstSize, 0.55f, 60, goalBurstColor);
        }
    }

    ParticleSystem CreateSystem(string name, float size, float lifetime, int burstCount, Color color, bool useTrails = false)
    {
        var go = new GameObject(name)
        {
            hideFlags = HideFlags.HideInHierarchy
        };
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = size * 6f;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(32, burstCount);
        main.gravityModifier = 0.2f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.15f;

        var trails = ps.trails;
        trails.enabled = useTrails;
        trails.lifetime = Mathf.Max(0.15f, lifetime * 0.4f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.5f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    void OnBallKicked(Vector3 position, Team team)
    {
        if (m_KickSystem == null)
        {
            return;
        }

        m_KickSystem.transform.position = position;
        var main = m_KickSystem.main;
        main.startColor = team == Team.Blue ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 0.3f, 0.8f, 1f);
        m_KickSystem.Play(true);
    }

    void OnGoalScored(Team scoringTeam, Vector3 goalPosition)
    {
        if (m_GoalSystem != null)
        {
            m_GoalSystem.transform.position = goalPosition + Vector3.up * 2f;
            var main = m_GoalSystem.main;
            main.startColor = scoringTeam == Team.Blue ? new Color(0f, 0.8f, 1f, 1f) : new Color(1f, 0.35f, 0.95f, 1f);
            m_GoalSystem.Play(true);
        }

        if (m_GoalPulseRoutine != null)
        {
            StopCoroutine(m_GoalPulseRoutine);
        }

        var lights = scoringTeam == Team.Blue ? blueGoalLights : purpleGoalLights;
        if (lights != null && lights.Length > 0)
        {
            m_GoalPulseRoutine = StartCoroutine(PulseGoalLights(lights, scoringTeam));
        }
    }

    IEnumerator PulseGoalLights(Light[] lights, Team team)
    {
        var baseColors = new Color[lights.Length];
        var baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            baseColors[i] = lights[i].color;
            baseIntensity[i] = lights[i].intensity;
        }

        Color accent = team == Team.Blue ? new Color(0f, 0.7f, 1f) : new Color(1f, 0.3f, 0.8f);

        float elapsed = 0f;
        while (elapsed < goalPulseDuration)
        {
            float t = elapsed / goalPulseDuration;
            float eval = pulseCurve != null ? pulseCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = Mathf.Lerp(baseIntensity[i], baseIntensity[i] * 3f, eval);
                lights[i].color = Color.Lerp(baseColors[i], accent, eval);
            }
            elapsed += Application.isPlaying ? Time.deltaTime : 0.02f;
            yield return null;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = baseIntensity[i];
            lights[i].color = baseColors[i];
        }
    }
}
