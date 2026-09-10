DAY13 FULL - VFX Graph 제어와 성능
=====================================

이번 DAY13은 DAY12와 다르게 VFX Graph 내부 비공개 Editor API를 사용하지 않습니다.
DAY12에서 이미 정상 동작 중인 VFX_GpuSpark 그래프를 보호하면서,
수업에서 필요한 Input System / 코드 제어 / 성능 비교를 public API로 구성합니다.

자동으로 하는 것
- DAY13 Input Actions Asset 생성
  * Gameplay
  * Point
  * Click
  * LowIntensity = <Keyboard>/1
  * HighIntensity = <Keyboard>/2
- DAY13 완성 씬 생성
- VFX_GpuSpark_Player 생성 및 DAY12 VFX_GpuSpark 연결
- VfxController
  * PlayerInput
  * Actions 연결
  * Default Map = Gameplay
  * Behavior = Send Messages
  * VfxIntensityController
- Low Rate 20 / Default 80 / High Rate 200
- VisualEffect.SetFloat("SpawnRate", value)
- Alive Particle Count 화면 Overlay
- Quality용 Context Menu
  * Apply Current Quality Preset
  * Disable VFX
  * Enable VFX
- 검증 메뉴

딱 한 번 직접 해야 하는 VFX Graph 작업
Assets/DAY13/Notes/DAY13_SpawnRate_Exposed_1Step.txt 참고:
Blackboard Float SpawnRate(80) -> Exposed -> Constant Spawn Rate Rate 입력에 연결 -> 저장

실행
1. ZIP의 Assets를 프로젝트 Assets에 병합
2. Unity 컴파일 완료
3. Tools > DAY13 FULL > 1 - Build Control + Performance Scene
4. 팝업이 SpawnRate가 없다고 하면 안내대로 Blackboard에서 SpawnRate 연결
5. Ctrl+S
6. Tools > DAY13 FULL > 3 - Validate DAY13
7. DAY13_VFXControlPerformance_COMPLETE 씬에서 Play
8. 키 1 = 20 / 키 2 = 200
9. 화면 Overlay에서 SpawnRate와 Alive Particles 비교

왜 SpawnRate Graph 노드만 수동인가?
DAY12에서 Unity 6.6 VFX Graph의 비공개 Editor API 차이 때문에 반복적인 컴파일 오류가 발생했습니다.
DAY13의 핵심은 Exposed Property를 직접 만들고 연결하는 과정 자체이기도 하므로,
정상 완성된 DAY12 그래프를 자동 코드로 다시 수정하지 않고 원문 절차대로 한 번 직접 연결하게 했습니다.

완료 후 삭제 가능
Assets/Editor/DAY13_FULL_AutoSetup.cs

남겨야 함
Assets/DAY13/Scripts/VfxIntensityController.cs
Assets/DAY13/Input/GraphicsInputActions_DAY13.inputactions
Assets/DAY13/Scenes/DAY13_VFXControlPerformance_COMPLETE.unity
