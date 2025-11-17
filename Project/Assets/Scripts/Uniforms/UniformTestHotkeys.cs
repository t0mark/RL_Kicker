using UnityEngine;

public class UniformTestHotkeys : MonoBehaviour
{
    public TeamUniformController ctrl;
    public Texture2D pattern1;
    public Texture2D pattern2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ctrl.ApplyPatternToBlue(pattern1, new Vector2(8,8), 1f);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ctrl.ApplyPatternToBlue(pattern2, new Vector2(12,12), 0.9f);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ctrl.ApplyPatternToPurple(pattern1, new Vector2(8,8), 1f);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            ctrl.ApplyPatternToPurple(pattern2, new Vector2(12,12), 0.9f);
    }
}