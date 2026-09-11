# DAY14 공간 배치 분석 - 마법 훈련장

## 1. 중앙 보호막 훈련 대상
| 항목 | 기록 |
|---|---|
| 역할 | 보호/피격 대상의 시인성 확인 |
| 위치 | 장면 중앙 |
| Shader | `SG_Portfolio_MagicShield` |
| 핵심 표현 | Fresnel + Emission |
| 카메라 거리 | 정면 Camera에서 가장 먼저 읽히는 중심 대상 |

## 2. FX_HealGlow
| 항목 | 기록 |
|---|---|
| 발생 위치 | 왼쪽 회복 패드 |
| 방향 | Prefab 원본의 상승 방향 |
| 크기 | Prefab Scale `(1,1,1)` |
| 지속 시간 | 회복 구역 시연 동안 Loop |
| 게임 의미 | 지속 회복/에너지 지점 |

## 3. FX_HitSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 오른쪽 Impact Pad의 Raycast Hit Point |
| 방향 | DAY10 Prefab의 Shape 설정 사용 |
| 크기 | Prefab Scale `(1,1,1)` |
| 지속 시간 | 짧은 One-shot |
| 게임 규칙 | 클릭이 한 번 확정될 때 HitSpark 한 번 생성 |

## 4. VFX_GpuSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 중앙 뒤쪽 마력 분수 장치 |
| 방향 | DAY12의 위로 솟고 중력으로 떨어지는 Fountain 형태 |
| 크기 | DAY12 Size Random 값 사용 |
| 지속 시간 | 지속 VFX / 개별 Particle Lifetime 사용 |
| 성능 손잡이 | SpawnRate 20 / 80 / 200 |
| 게임 의미 | 훈련장의 마력 공급 장치 |

## 장면 색 방향
- Environment: 어두운 청회색
- Accent: 청록/하늘색 Emission
- Shield: DAY05 보호막 표현
- Hit/VFX 강조: 주황 계열 Particle
- 목적: 서로 다른 DAY 결과물을 하나의 장면처럼 읽히게 함
