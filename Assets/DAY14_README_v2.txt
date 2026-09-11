DAY14 FINAL v2 - 기존 작업 통합판
=================================

이번 버전은 새 마법 훈련장, 새 Sphere/Capsule, 새 프로토타입 맵을 임의로 만들지 않습니다.

원문 흐름 그대로:
1. 기존 GraphicsLab 씬을 GraphicsPortfolio로 복제
2. 기존 씬의 원래 오브젝트를 Environment 아래 정리
3. 기존 Mesh 하나를 ShaderTargets로 이동하고 DAY05~08 Shader Graph 복제본 Material 적용
4. DAY10 Particle Prefab 2종을 ParticleEffects에 배치
5. DAY12 VFX_GpuSpark를 VfxEffects에 배치
6. DAY11 ClickEffectSpawner + DAY13 VfxIntensityController를 EffectInput에 연결
7. Ground 클릭 / 숫자 1 / 숫자 2 Play Test
8. 공간 배치 / 플레이 규칙 / 검증 기록 Markdown 생성

사용법
1. ZIP의 Assets를 프로젝트 Assets에 병합
2. Unity 컴파일 완료
3. Tools > DAY14 FINAL > 1 - Build From Existing Work
4. Assets/DAY14/Scenes/GraphicsPortfolio.unity 열기
5. Play
6. Ground 좌클릭 -> HitSpark
7. 숫자 1 -> SpawnRate 20
8. 숫자 2 -> SpawnRate 200
9. Tools > DAY14 FINAL > 2 - Validate

완료 후 삭제 가능
Assets/Editor/DAY14_PortfolioIntegrationV2.cs

남겨야 함
Assets/DAY14/Shaders
Assets/DAY14/Materials
Assets/DAY14/Settings
Assets/DAY14/Scenes
Assets/DAY14/Documentation


[v3 FIX]
- MissingReferenceException: InputActionAsset has been destroyed 오류 수정.
- 원인 방지: Edit Mode에서 PlayerInput.actions public setter를 사용하지 않음.
- PlayerInput의 m_Actions / m_DefaultActionMap / m_NotificationBehavior를 SerializedObject로 저장.
- 검증도 PlayerInput public getter 대신 직렬화된 필드를 읽도록 변경.
