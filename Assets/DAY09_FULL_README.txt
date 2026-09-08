DAY09 FULL - Particle System 기초 전체 재구축

이 툴은 DAY09 문서의 핵심 과정과 누락 가능했던 확인 항목을 한 씬에 모두 구성합니다.

생성:
Assets/DAY09/
  Scenes/DAY09_ParticleBasics_COMPLETE.unity
  Materials/Mat_HitSpark.mat
  Materials/Mat_Dust.mat
  Materials/Mat_Explosion.mat
  Textures/T_SoftParticle.png
  Scripts/DAY09_HitPointDemo.cs
  Scripts/DAY09_LocalWorldMover.cs

씬 Hierarchy:
01_HitEffect_Core
- FX_HitSpark_Test
- HitTarget_Capsule
- 실제 Raycast Hit Point 1회 재생

02_3DSpace_Analysis
- Weapon Tip / Target Center / Wall Surface / Camera Distance 비교 오브젝트

03_SimulationSpace_Comparison
- Local_SimulationSpace
- World_SimulationSpace
- 부모 오브젝트가 좌우 이동하면서 차이를 확인

04_Extra_Explosion_Dust
- FX_Dust_Test
- FX_Explosion_Test

HitSpark 설정:
- Duration 짧게
- Looping OFF
- Start Lifetime / Speed / Size
- Emission Rate over Time = 0
- Burst 1개
- Cone Shape
- Color over Lifetime: 밝음 -> 투명
- Size over Lifetime: 커졌다가 0
- Velocity over Lifetime: X/Y/Z 모두 Constant 모드
- Renderer / URP Particle Material
- Simulation Space = World
- Stop Action = None

사용:
1) ZIP의 Assets 내용을 기존 GameGraphics 프로젝트 Assets에 병합
2) Unity 컴파일 완료
3) Tools > DAY09 FULL > Build Complete DAY09
4) Assets/DAY09/Scenes/DAY09_ParticleBasics_COMPLETE.unity 확인
5) Play Mode:
   - HitTarget에 약 2초마다 실제 Raycast Hit Point 기준 HitSpark가 1회 재생
   - Local/World Simulation Space 차이 확인
   - Dust/Explosion은 Play On Awake로 1회 재생
6) 완료 후 Assets/Editor/DAY09_FULL_AutoSetup.cs 삭제 가능
   (Assets/DAY09/Scripts의 두 런타임 스크립트는 씬에서 사용하므로 유지)
