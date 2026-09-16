# Inspector Graph Play Mode 검증 기록

## 실행 환경

| 항목 | 기록 |
|---|---|
| Unity 버전 | Builder가 생성한 Runtime Settings Record 참고 |
| Render Pipeline Asset |  |
| Quality Level |  |
| 테스트 해상도 | 1920 x 1080 권장 |
| 테스트 날짜 |  |

## 필수 검증

| 번호 | 확인 항목 | 결과 | 증거 스크린샷 파일명 |
|---:|---|---|---|
| 1 | Unity 6 URP에서 Scene이 열리고 Console Error가 없다 | 미확인 |  |
| 2 | MagicShield에 Mat MagicShield가 적용되어 있다 | 미확인 |  |
| 3 | Shader Graph에서 Fresnel Emission Time Alpha 연결이 보인다 | 미확인 |  |
| 4 | PBR 두 Material의 Metallic Smoothness Emission 값이 다르다 | 미확인 |  |
| 5 | FX HitSpark와 FX HealGlow Prefab에 Particle System이 있다 | 미확인 |  |
| 6 | 한 번의 Impact Pad 클릭에 HitSpark가 한 번만 생성된다 | 미확인 |  |
| 7 | HealGlow가 회복 패드에서 Loop된다 | 미확인 |  |
| 8 | VFX Graph의 Spawn Initialize Update Output이 연결되어 있다 | 미확인 |  |
| 9 | SpawnRate가 Float Exposed Property이고 Constant Spawn Rate에 연결되어 있다 | 미확인 |  |
| 10 | 숫자 1과 2를 눌렀을 때 VFX 밀도가 달라진다 | 미확인 |  |
| 11 | Runtime Settings Record가 생성되어 있다 | 미확인 |  |
| 12 | Validate 메뉴 결과에 FAIL이 없다 | 미확인 |  |

## 중복 재생 테스트

1. Impact Pad를 한 번 클릭한다.
2. Hierarchy에서 FX HitSpark Clone이 한 개만 생성되는지 확인한다.
3. 빠르게 두 번 클릭했을 때 입력 간격보다 짧은 중복 생성이 차단되는지 확인한다.
4. Impact Pad 밖을 클릭했을 때 이펙트가 생성되지 않는지 확인한다.

| 테스트 | 예상 결과 | 실제 결과 |
|---|---|---|
| 한 번 클릭 | HitSpark 한 개 |  |
| Pad 밖 클릭 | 생성 없음 |  |
| 동일 프레임 중복 입력 | 한 개만 생성 |  |
| Particle 종료 | Clone 자동 제거 |  |

## 성능 비교

| 설정 | SpawnRate | 관찰 FPS | 화면 복잡도 | 판정 |
|---|---:|---:|---|---|
| Low | 20 |  |  |  |
| Default | 80 |  |  |  |
| High | 200 |  |  |  |

## 권장 스크린샷

1. Game View 전체 장면
2. Shader Graph 전체 흐름과 Blackboard
3. PBR Material 두 Inspector
4. FX HitSpark Prefab Inspector
5. FX HealGlow Prefab Inspector
6. VFX Graph Spawn Initialize Update Output과 SpawnRate
7. Play Mode HitSpark 발생 장면
8. Validate Console 결과
