# DAY14 Graphics Portfolio - 마법 훈련장 검증 기록

## 장면 주제
**마법 훈련장 - 마력 제어 실험실**

DAY05~13에서 만든 결과물을 단순히 일렬로 전시하지 않고,
보호막 훈련 / 회복 구역 / 타격 시험 / 마력 분수라는 역할로 재배치합니다.

## 원본 보존
- Base Scene: `Assets/DAY01/Scenes/GraphicsLab.unity`
- DAY05 Source Graph: `Assets/DAY05/Shaders/SG_Shield.shadergraph`
- 원본 Graph는 수정하지 않고 `SG_Portfolio_MagicShield` 복제본 사용.
- 기존 GraphicsLab 오브젝트는 `LegacyReference_Disabled` 아래 비활성 보존.

## 최종 요구사항 대응

| DAY14 요구 | 구현 |
|---|---|
| Shader Graph 1개 이상 | `SG_Portfolio_MagicShield` |
| 표면 표현 2개 이상 | DAY05 Shield의 Fresnel + Emission |
| Particle System 2개 이상 | `FX_HitSpark`, `FX_HealGlow` |
| VFX Graph 1개 이상 | `VFX_GpuSpark` |
| 코드 연동 | Impact Pad 좌클릭 -> HitSpark 1회 |
| 공간 배치 분석 | `DAY14_Spatial_Placement.md` |
| 플레이 규칙 | 한 번의 타격 클릭 -> HitSpark 한 번 |
| 검증 기록 | 이 문서 + Validate Menu |

## Hierarchy
- `Environment`
- `ShaderTargets`
  - 중앙 보호막 훈련 대상
- `ParticleEffects`
  - 왼쪽 회복 구역
  - 오른쪽 타격 시험 구역
- `VfxEffects`
  - 뒤쪽 마력 분수
- `EffectInput`
- `Verification`

## Play Mode
1. 중앙 Shield의 Fresnel/Emission 표현을 Camera 거리에서 확인.
2. 왼쪽 HealGlow가 회복 구역 역할로 보이는지 확인.
3. 오른쪽 Impact Pad를 좌클릭하여 HitSpark가 클릭 위치에 한 번 생성되는지 확인.
4. 뒤쪽 VFX_GpuSpark가 마력 분수처럼 위로 솟는지 확인.
5. 숫자 `1` -> SpawnRate 20.
6. 숫자 `2` -> SpawnRate 200.
7. Console 빨간 Error가 없는지 확인.

## Input
- Input Actions: `GraphicsInputActions_DAY13`
- PlayerInput Default Map: Gameplay
- Behavior: Send Messages
- Point / Click / LowIntensity / HighIntensity 사용

## 성능 설명
- 프레임 저하: SpawnRate를 먼저 줄임.
- 동시에 너무 많은 입자: Lifetime 감소.
- 화면 밖 VFX 계산: Bounds 확인.
- 지나치게 밝음: Color / Alpha 조절.
- 저사양: VFX 약화 또는 비활성화.
