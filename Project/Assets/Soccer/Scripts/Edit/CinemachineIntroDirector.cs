using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineIntroDirector : MonoBehaviour
{
    [System.Serializable]
    public class Shot
    {
        public CinemachineCamera camera;
        public float duration = 2f;
    }

    [Header("Intro Sequence")]
    public Shot[] shots;
    public CinemachineCamera gameplayCamera;
    public int resetBeforeThisShot;

    [Header("Systems to Disable During Intro")]
    public MonoBehaviour[] disableDuringIntro;

    [Header("Optional UI")]
    public ScoreUIManager scoreUI;

    [Header("Skip Settings")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;

    CinemachineBrain _brain;
    CinemachineBlendDefinition _originalBlend;
    SoccerEnvController env;

    IEnumerator Start()
    {
        env = FindFirstObjectByType<SoccerEnvController>();
        env.resetAllowed = false;

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            _brain = mainCam.GetComponent<CinemachineBrain>();
            if (_brain != null)
            {
                _originalBlend = _brain.DefaultBlend;
                _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }
        }

        FreezeAllPlayers();

        foreach (var mb in disableDuringIntro)
        {
            if (mb != null) mb.enabled = false;
        }

        int originalGameplayPriority = 0;
        if (gameplayCamera != null)
        {
            originalGameplayPriority = gameplayCamera.Priority;
            gameplayCamera.Priority = 0;
        }

        if (shots != null)
        {
            foreach (var s in shots)
            {
                if (s.camera == null) continue;

                s.camera.Priority = 0;
                StopDolly(s.camera);
            }
        }

        if (shots != null && shots.Length > 0)
        {
            yield return StartCoroutine(PlayIntro());
        }

        if (_brain != null)
        {
            _brain.DefaultBlend = _originalBlend;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.Priority =
                (originalGameplayPriority > 0) ? originalGameplayPriority : 10;
        }

        if (_brain != null)
        {
            yield return null;
            while (_brain.IsBlending)
                yield return null;

            yield return new WaitForSeconds(_brain.DefaultBlend.Time);
        }

        foreach (var mb in disableDuringIntro)
        {
            if (mb != null) mb.enabled = true;
        }

        if (scoreUI != null)
        {
            scoreUI.ShowWithFade();
        }

        env.resetAllowed = true;
        UnfreezeAllPlayers();
    }

    IEnumerator PlayIntro()
    {
        if (shots != null)
        {
            int i = 0;
            foreach (var s in shots)
            {
                if (s.camera == null) continue;

                if (i == resetBeforeThisShot)
                {
                    if (env != null)
                    {
                        env.resetAllowed = true;
                        env.ResetScene();
                    }
                }

                StartDolly(s.camera);
                s.camera.Priority = 100;

                float t = 0f;
                bool skipped = false;
                while (t < s.duration)
                {
                    if (allowSkip && Input.GetKeyDown(skipKey))
                    {
                        skipped = true;
                        break;
                    }
                    t += Time.deltaTime;
                    yield return null;
                }

                s.camera.Priority = 0;

                if (skipped)
                    break;
                i++;
            }
            if (i <= resetBeforeThisShot || resetBeforeThisShot < 0)
            {
                if (env != null)
                {
                    env.resetAllowed = true;
                    env.ResetScene();
                }
            }
        }
    }

    void StopDolly(CinemachineCamera cam)
    {
        if (cam == null) return;

        var dolly = cam.GetComponent<CinemachineSplineDolly>();
        if (dolly != null)
        {
            dolly.enabled = false;
        }
    }

    void StartDolly(CinemachineCamera cam)
    {
        if (cam == null) return;

        var dolly = cam.GetComponent<CinemachineSplineDolly>();
        if (dolly != null)
        {
            dolly.CameraPosition = 0f;
            dolly.enabled = true;
        }
    }

    void FreezeAllPlayers()
    {
        var agents = FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None);

        foreach (var ag in agents)
        {
            var rb = ag.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var bp = ag.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (bp) bp.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;

            var manual = ag.GetComponent<ManualController>();
            if (manual) manual.enabled = false;

            ag.manualOverride = true;
        }
    }

    void UnfreezeAllPlayers()
    {
        var agents = FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None);

        foreach (var ag in agents)
        {
            var rb = ag.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
            }

            var bp = ag.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (bp) bp.BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;

            var manual = ag.GetComponent<ManualController>();
            if (manual) manual.enabled = true;

            ag.manualOverride = false;
        }
    }
}
