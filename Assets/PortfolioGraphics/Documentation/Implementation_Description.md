# 게임 그래픽 프로그래밍 포트폴리오 구현 설명서

## 프로젝트 개요

- 프로젝트명: 마법 훈련장 그래픽 쇼케이스
- 개발 환경: Unity 6 URP C# Shader Graph Particle System Visual Effect Graph
- 구현 씬: `GraphicsPortfolio_MagicTrainingGround`
- 목표: 보호막 훈련 회복 구역 타격 시험 마력 분수를 하나의 작은 플레이 장면으로 구성하고 그래픽 표현을 게임 사건과 연결한다.

## 그래픽 콘셉트

어두운 청회색 석재로 구성된 마법 훈련장 안에서 보호막과 마력 장치를 시험하는 장면이다. 중앙 보호막은 청록색 Fresnel과 Emission으로 가장 먼저 보이게 하고 타격 이펙트와 마력 분수는 주황색 계열로 분리해 기능을 구분한다. 왼쪽 회복 구역은 천천히 상승하는 청록 입자로 안전하고 지속적인 효과를 전달하며 오른쪽 타격 패드는 짧고 빠른 불꽃으로 즉시 발생한 공격 사건을 보여준다. 장면의 구조물은 낮은 채도와 낮은 Emission을 사용해 핵심 이펙트가 배경에 묻히지 않도록 설계했다.

## 필수 구현 대응

| 요구사항 | 구현 결과 |
|---|---|
| Shader Graph Material | `SG_MagicShield`와 `Mat_MagicShield` |
| 표면 표현 두 개 이상 | Fresnel Emission Time Pulse Alpha |
| PBR Material 비교 | MatteStone과 ArcaneMetal의 Metallic Smoothness Emission 비교 |
| Particle System 두 개 이상 | `FX_HitSpark`와 `FX_HealGlow` Prefab |
| VFX Graph 한 개 이상 | `VFX_GpuSpark` |
| 코드 연동 | Impact Pad 좌클릭 Raycast 성공 시 HitSpark 생성 |
| 중복 생성 방지 | `wasPressedThisFrame` 동일 프레임 차단 0.08초 간격 |
| 구현 설정 기록 | 이 문서와 `Runtime_Settings_Record.md` |
| 검증 기록 | `Verification_Checklist.md`와 Validate 메뉴 |

## 장면 배치와 플레이 규칙

카메라는 `(0 4.4 -10.5)`에 배치하고 FOV는 50으로 설정한다.

| 요소 | 발생 위치 | 방향 | 크기 | 카메라 거리 | 지속 시간 | 전달하는 규칙 |
|---|---|---|---|---|---|---|
| MagicShield | 중앙 `(0 1.25 0.65)` | 카메라와 표면 각도에 따른 Rim | Sphere Scale 2.1 | 약 11.5 m | 지속 | 중앙 대상이 보호 또는 강화 상태임을 표시 |
| FX HealGlow | 왼쪽 `(-3.05 0.32 1.15)` | World Y 상승 | Start Size 0.08에서 0.22 | 약 12.7 m | Loop 개별 0.9에서 1.5초 | 해당 구역이 지속 회복 지점임을 표시 |
| FX HitSpark | 오른쪽 Impact Pad의 Raycast Hit Point | 충돌 Normal 기준 방사 | Start Size 0.05에서 0.13 | 약 12.7 m | 전체 약 0.7초 이내 | 클릭이 타격으로 확정됐음을 즉시 표시 |
| VFX GpuSpark | 뒤쪽 `(0 0.35 3.55)` | 위쪽과 바깥쪽으로 분사 | Size 0.03에서 0.12 | 약 14.6 m | 지속 개별 0.4에서 1.2초 | 마력 공급 장치의 작동 상태와 강도를 표시 |
| PBR Sample | 앞쪽 좌우 `x -3.4와 3.4` | 같은 조명과 시점 | Sphere Scale 1.35 | 약 10.4 m | 지속 | 재질 수치 차이가 반사와 밝기에 미치는 영향을 비교 |

## Shader Graph 핵심 연결

`SG_MagicShield`는 URP Lit 기반의 Transparent Surface를 사용한다.

1. Fresnel Effect의 Power에 `RimPower`를 연결한다.
2. Fresnel 결과와 `ShieldColor`를 Multiply한다.
3. 결과에 `EmissionStrength`를 곱해 Emission으로 출력한다.
4. Time과 `PulseSpeed`를 Multiply하고 Sine과 Remap을 거쳐 밝기 변화에 사용한다.
5. Fresnel 결과에 `AlphaStrength`를 곱해 Alpha로 출력한다.

| Property | 값 | 역할 |
|---|---:|---|
| ShieldColor | `(0.15 0.65 1.00 1.00)` | 보호막 기본색 |
| RimPower | 3.0 | 외곽선 폭과 집중도 |
| EmissionStrength | 2.0 | 외곽 발광 강도 |
| AlphaStrength | 0.45 | 보호막 투명도 |
| PulseSpeed | 2.0 | 발광 맥동 속도 |
| Metallic | 0.0 | 비금속 보호막 표면 |
| Smoothness | 0.5 | 기본 반사 선명도 |

사용한 필수 표현은 Fresnel과 Emission이며 Time Pulse와 Alpha를 추가해 정지된 구체가 아니라 작동 중인 보호막처럼 보이게 했다.

## PBR Material 비교

두 Sphere는 같은 장면과 같은 광원에서 비교한다.

| Material | Metallic | Smoothness | Emission | 예상 결과 |
|---|---:|---:|---|---|
| Mat PBR MatteStone | 0.05 | 0.15 | 없음 | 반사가 넓고 흐리며 거친 석재처럼 보임 |
| Mat PBR ArcaneMetal | 0.85 | 0.80 | 청록 `(0 1.25 1.65)` | 반사가 선명하고 금속성이 강하며 자체 발광이 보임 |

## Particle System 설정

### FX HitSpark

| Module | 값 |
|---|---|
| Main Duration | 0.35초 |
| Loop | Off |
| Start Lifetime | 0.18에서 0.32초 |
| Start Speed | 2.8에서 4.8 |
| Start Size | 0.05에서 0.13 |
| Max Particles | 64 |
| Emission | Time 0에서 Burst 28 |
| Shape | Hemisphere Radius 0.08 |
| Color over Lifetime | 노랑에서 주황으로 변화하며 Alpha 0 |
| Size over Lifetime | 1에서 0 |
| Renderer | Stretched Billboard |

타격은 짧은 시간에 많은 입자가 한 번 발생해야 하므로 Rate over Time 대신 Burst를 사용한다.

### FX HealGlow

| Module | 값 |
|---|---|
| Main Duration | 2.0초 |
| Loop | On |
| Start Lifetime | 0.90에서 1.50초 |
| Start Speed | 0 |
| Start Size | 0.08에서 0.22 |
| Max Particles | 96 |
| Emission Rate over Time | 24 |
| Shape | Box `(1.4 0.05 1.4)` |
| Velocity over Lifetime | World Y 0.35에서 0.80 |
| Noise | Strength 0.18 Frequency 0.35 |
| Color over Lifetime | 청록과 하늘색 Alpha Fade |
| Renderer | Billboard |

회복 구역은 지속 상태이므로 Loop와 Rate over Time을 사용하고 낮은 상승 속도로 공격 이펙트와 구분한다.

## Visual Effect Graph 흐름

| Context | 핵심 설정 |
|---|---|
| Spawn | Constant Spawn Rate 기본 80과 Exposed Float `SpawnRate` |
| Initialize | Capacity 1024 Lifetime 0.4에서 1.2 Sphere Position Upward Velocity Size 0.03에서 0.12 Orange Color |
| Update | Force Drag Age over Lifetime Size 변화 |
| Output | Output Particle Quad Additive 계열 표현 |

`SpawnRate`는 Blackboard Float Property로 만들고 Exposed를 켠 뒤 Constant Spawn Rate의 Rate 입력에 연결한다. 코드에서 숫자 1은 20 숫자 2는 200 숫자 3은 80을 전달한다.

## 코드 연동과 중복 방지

`PortfolioEffectController`는 New Input System의 `Mouse.current.leftButton.wasPressedThisFrame`을 확인한다. 마우스 위치에서 Camera Raycast를 실행하고 `PortfolioImpact` Layer의 Impact Pad에 맞았을 때만 `FX_HitSpark` Prefab을 생성한다. 한 번의 사건에서 중복 생성되지 않도록 마지막 생성 Frame을 저장하고 다음 생성 가능 시간까지 0.08초를 둔다. 생성된 Particle은 Duration과 최대 Lifetime을 더한 시간이 지나면 자동 제거한다.

## URP와 Quality 기록

Builder 실행 시 `Runtime_Settings_Record.md`에 Unity Version Render Pipeline Asset Quality Level VSync Anti Aliasing Shadow Distance SpawnRate 노출 여부를 기록한다. 제출 전 해당 기록과 Project Settings의 실제 Inspector 화면이 일치하는지 확인한다.

## 성능 조절 항목

| 상황 | 우선 조절 값 | 기준 |
|---|---|---|
| VFX 입자가 너무 많음 | SpawnRate | 200에서 80 또는 20으로 낮춤 |
| 화면에 입자가 오래 남음 | Lifetime | VFX 1.2와 Heal 1.5의 Max 값을 낮춤 |
| 타격 순간 과도한 입자 | Burst Count | 28을 낮춤 |
| 회복 구역이 복잡함 | Rate over Time | 24를 낮춤 |
| 화면이 지나치게 밝음 | Emission과 Alpha | Emission Strength와 Particle Alpha를 낮춤 |
| 화면 밖에서도 비용 발생 | VFX Bounds | 장치 범위에 맞게 Bounds를 축소 |

## 제출 파일

- Unity 6 URP 프로젝트 또는 Assets 통합 폴더
- `GraphicsPortfolio_MagicTrainingGround.unity`
- `SG_MagicShield.shadergraph`
- `FX_HitSpark.prefab`
- `FX_HealGlow.prefab`
- `VFX_GpuSpark.vfx`
- `PortfolioEffectController.cs`
- 구현 설명서
- Inspector Graph Play Mode 검증 기록
