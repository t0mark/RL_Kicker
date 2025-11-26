using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamUniformController : MonoBehaviour
{
    [Header("Team Roots (add multiple)")]
    public List<Transform> blueRoots = new();
    public List<Transform> purpleRoots = new();

    [Header("Team Materials (Chibi_* templates)")]
    public Material matBlue;   // Chibi_Blue
    public Material matPurple; // Chibi_Purple

    [Header("Default Team Colors")]
    [SerializeField] Color baseBlue   = new Color32(0x21, 0x96, 0xF3, 0xFF); // #2196F3
    [SerializeField] Color basePurple = new Color32(0x8D, 0x6D, 0xC8, 0xFF); // #8D6DC8

    [Header("Target filters (by MATERIAL name only)")]
    [Tooltip("Material(slot) name must contain ANY of these tokens to be treated as clothing.\n예: \"M_Chibi_Shirt\", \"M_Chibi_Pants\" → tokens: shirt, pants")]
    [SerializeField] string[] includeMaterialTokens = { "shirt", "pants" };

    [Header("Default Tiling")]
    [SerializeField] Vector2 defaultTiling = new Vector2(4, 4);

    // ---- internal state ----
    class RendSlots
    {
        public Renderer r;
        public Material[] original;      // 원래 슬롯들
        public List<int> targetSlots = new(); // 교체할 슬롯 인덱스만
    }

    List<RendSlots> blueTargets   = new();
    List<RendSlots> purpleTargets = new();

    void Awake()
    {
        blueTargets   = CollectTargets(blueRoots);
        purpleTargets = CollectTargets(purpleRoots);

        // 시작 시: 의복 전체를 팀 템플릿으로 교체(패턴은 끔, 팀색만)
        ApplyTeamDefault(blueTargets,   matBlue,   baseBlue);
        ApplyTeamDefault(purpleTargets, matPurple, basePurple);
    }

    // ------------ public API (브리지/버튼에서 호출) ------------
    public void ApplyPatternToBlue(Texture2D tex, Vector2 tiling, float strength = 1f)
        => ApplyPattern(blueTargets, matBlue, baseBlue, tex, tiling, strength);

    public void ApplyPatternToPurple(Texture2D tex, Vector2 tiling, float strength = 1f)
        => ApplyPattern(purpleTargets, matPurple, basePurple, tex, tiling, strength);

    public void SetTeamDefaultBlue()   => ApplyTeamDefault(blueTargets,   matBlue,   baseBlue);
    public void SetTeamDefaultPurple() => ApplyTeamDefault(purpleTargets, matPurple, basePurple);
    public void ResetAllToDefaults()   { SetTeamDefaultBlue(); SetTeamDefaultPurple(); }

    // ------------ collectors & helpers ------------
    List<RendSlots> CollectTargets(List<Transform> roots)
    {
        var list = new List<RendSlots>();

        foreach (var root in roots)
        {
            if (!root) continue;

            // 이 팀 밑에 있는 모든 Renderer (SkinnedMeshRenderer 포함)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                var info = new RendSlots
                {
                    r        = r,
                    original = mats.ToArray()
                };

                // ★ 머티리얼 이름만 보고 슬롯 결정
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    var mName = (m ? m.name : string.Empty).ToLowerInvariant();

                    if (NameHasAny(mName, includeMaterialTokens))
                        info.targetSlots.Add(i);
                }

                // 이 Renderer 안에 교체 대상 슬롯이 하나 이상 있을 때만 리스트에 추가
                if (info.targetSlots.Count > 0)
                    list.Add(info);
            }
        }

        return list;
    }

    static bool NameHasAny(string name, string[] tokens)
    {
        if (tokens == null || tokens.Length == 0) return false;
        foreach (var t in tokens)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            if (name.Contains(t.ToLowerInvariant())) return true;
        }
        return false;
    }

    void ApplyTeamDefault(List<RendSlots> targets, Material teamMat, Color baseColor)
    {
        if (!teamMat) return;

        // 템플릿 머티리얼 값 세팅: 팀색 + 패턴 끔
        if (teamMat.HasProperty("_BaseColor"))       teamMat.SetColor("_BaseColor", baseColor);
        if (teamMat.HasProperty("_PatternStrength")) teamMat.SetFloat("_PatternStrength", 0f);
        if (teamMat.HasProperty("_PatternTiling"))   teamMat.SetVector("_PatternTiling", new Vector4(defaultTiling.x, defaultTiling.y, 0, 0));
        if (teamMat.HasProperty("_PatternTex"))      teamMat.SetTexture("_PatternTex", null);

        foreach (var t in targets)
        {
            if (!t.r) continue;

            var mats = t.r.sharedMaterials.ToArray();

            // 의복 대상 슬롯 전부 팀 머티리얼로 교체
            foreach (var idx in t.targetSlots)
                if (idx >= 0 && idx < mats.Length)
                    mats[idx] = teamMat;

            t.r.sharedMaterials = mats;
        }
    }

    void ApplyPattern(List<RendSlots> targets, Material teamMat, Color baseColor,
                      Texture2D tex, Vector2 tiling, float strength)
    {
        if (!teamMat) return;

        // 텍스처/파라미터 세팅
        if (teamMat.HasProperty("_BaseColor"))       teamMat.SetColor("_BaseColor", baseColor);
        if (teamMat.HasProperty("_PatternStrength")) teamMat.SetFloat("_PatternStrength", Mathf.Clamp01(strength));
        if (teamMat.HasProperty("_PatternTiling"))   teamMat.SetVector("_PatternTiling", new Vector4(tiling.x, tiling.y, 0, 0));
        if (teamMat.HasProperty("_PatternTex"))
        {
            if (tex)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                teamMat.SetTexture("_PatternTex", tex);
            }
            else
            {
                teamMat.SetTexture("_PatternTex", null);
            }
        }

        foreach (var t in targets)
        {
            if (!t.r) continue;

            var mats = t.r.sharedMaterials.ToArray();

            foreach (var idx in t.targetSlots)
                if (idx >= 0 && idx < mats.Length)
                    mats[idx] = teamMat;

            t.r.sharedMaterials = mats;
        }
    }

    // 디버그용: 어떤 렌더러/슬롯이 대상인지 확인
    [ContextMenu("DEBUG/List Targets")]
    void DebugListTargets()
    {
        void Dump(string team, List<RendSlots> list)
        {
            foreach (var t in list)
            {
                if (!t.r) continue;
                var slots = string.Join(",", t.targetSlots);
                Debug.Log($"[{team}] Renderer='{t.r.name}' slots=[{slots}] mats={t.r.sharedMaterials.Length}");
            }
        }

        Debug.Log("=== UNIFORM TARGETS ===");
        Dump("BLUE", blueTargets);
        Dump("PURPLE", purpleTargets);
    }
}