using UnityEngine;
using UnityEngine.VFX;

public class DAY14MagicTrainingHUD : MonoBehaviour
{
    [SerializeField] private VisualEffect energyVfx;
    [SerializeField] private bool showHud = true;

    private void OnGUI()
    {
        if (!showHud)
            return;

        float spawnRate = 0f;
        bool hasSpawnRate =
            energyVfx != null &&
            energyVfx.HasFloat("SpawnRate");

        if (hasSpawnRate)
            spawnRate = energyVfx.GetFloat("SpawnRate");

        GUI.Box(new Rect(14, 14, 410, 126), "DAY14 - 마법 훈련장 / Magic Training Ground");
        GUI.Label(new Rect(28, 42, 380, 20), "오른쪽 타격 패드 좌클릭 : FX_HitSpark");
        GUI.Label(new Rect(28, 64, 380, 20), "숫자 1 : 마력 출력 LOW (SpawnRate 20)");
        GUI.Label(new Rect(28, 86, 380, 20), "숫자 2 : 마력 출력 HIGH (SpawnRate 200)");

        string vfxText = hasSpawnRate
            ? $"에너지 분수 SpawnRate : {spawnRate:0}"
            : "에너지 분수 SpawnRate : 연결 확인 필요";

        GUI.Label(new Rect(28, 108, 380, 20), vfxText);
    }
}
