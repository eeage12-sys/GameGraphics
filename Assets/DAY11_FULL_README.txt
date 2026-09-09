DAY11 FULL - 이펙트와 게임 코드 연동

목표
게임 사건 발생 -> 위치 결정 -> 이펙트 생성 -> 재생 -> 일정 시간 뒤 제거

수업 원문 기준으로 포함한 과정

1. Input Actions Asset
Assets/DAY11/Input/GraphicsInputActions.inputactions

Gameplay Action Map
- Point
  Type: Value
  Control Type: Vector2
  Binding: <Pointer>/position
- Click
  Type: Button
  Binding: <Mouse>/leftButton

2. EffectInput GameObject
- PlayerInput
  Actions: GraphicsInputActions
  Default Map: Gameplay
  Behavior: Send Messages
- ClickEffectSpawner

3. ClickEffectSpawner 연결
- Target Camera: Main Camera
- Effect Prefab: DAY10/Prefabs/Effects/FX_HitSpark
- Ground Mask: Ground Layer만

4. Ground
- Plane
- Layer: Ground
- Collider 활성

5. 실제 코드 흐름
OnPoint
-> pointerPosition 저장

OnClick
-> value.isPressed 확인
-> targetCamera.ScreenPointToRay
-> Physics.Raycast (100m, Ground Mask)
-> Instantiate FX_HitSpark at hit.point
-> effect.Play()
-> Destroy(duration + startLifetime.constantMax)

6. Play Mode 확인
- Game 탭에서 마우스를 움직임
- Plane을 클릭
- 클릭한 Ground 위치에서 FX_HitSpark 1회 생성
- Ground 밖 클릭은 생성되지 않음
- Console Error 없음 확인
- 재생 종료 뒤 생성된 GameObject 정리 확인

실행
1) ZIP의 Assets 내용을 기존 GameGraphics 프로젝트 Assets에 병합
2) Unity 컴파일 완료
3) Tools > DAY11 FULL > Build Complete DAY11
4) Assets/DAY11/Scenes/DAY11_EffectCodeIntegration_COMPLETE.unity 열기
5) Game 탭 -> Play
6) Plane 클릭

검증
Tools > DAY11 FULL > Validate DAY11

Input Actions 직접 확인
Tools > DAY11 FULL > Open GraphicsInputActions
또는
Assets/DAY11/Input/GraphicsInputActions.inputactions 더블클릭

완료 후 삭제 가능
Assets/Editor/DAY11_FULL_AutoSetup.cs

남겨야 함
Assets/DAY11/Input/GraphicsInputActions.inputactions
Assets/DAY11/Scripts/ClickEffectSpawner.cs
Assets/DAY11/Scenes/DAY11_EffectCodeIntegration_COMPLETE.unity

주의
DAY11은 DAY10에서 만든 FX_HitSpark Prefab을 실제로 사용합니다.
DAY10 Prefab이 없으면 임의의 대체 이펙트를 만들지 않고 생성 오류를 표시합니다.
