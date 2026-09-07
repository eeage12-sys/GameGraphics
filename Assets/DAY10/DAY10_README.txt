DAY 10 - Particle System 모듈과 프리팹화

기존 GameGraphics 프로젝트에 적용:
1) 이 ZIP의 Assets 폴더를 기존 GameGraphics 프로젝트 루트에 병합합니다.
2) Unity가 컴파일/임포트를 끝낼 때까지 기다립니다.
3) 상단 메뉴 Tools > DAY10 > Finalize Particle Prefab Demo 를 실행합니다.
4) 생성되는 실제 Prefab Asset:
   Assets/GameGraphics/Prefabs/Effects/FX_HitSpark.prefab
   Assets/GameGraphics/Prefabs/Effects/FX_HealGlow.prefab
   Assets/GameGraphics/Prefabs/Effects/FX_ExplosionSmall.prefab
5) 데모 씬:
   Assets/Scenes/DAY10_ParticlePrefabs.unity

확인 포인트:
- 세 프리팹 모두 Transform Scale = (1,1,1)
- FX_HitSpark: 짧은 Lifetime / 빠른 Speed / Burst
- FX_HealGlow: 초록색 / 천천히 위로 상승
- FX_ExplosionSmall: Burst / 큰 Size 변화
- Project 창의 파란 Prefab Asset을 원하는 씬으로 직접 드래그해서 재사용할 수 있습니다.
- Tools 메뉴는 자동 생성/편의용이며, 생성 후에는 Prefab과 Particle System Inspector를 직접 수정해도 됩니다.

재생 메뉴:
Tools > DAY10 > Replay All Effect Prefabs
Tools > DAY10 > Replay HitSpark
Tools > DAY10 > Replay HealGlow
Tools > DAY10 > Replay ExplosionSmall

주의:
- DAY10은 Prefab화와 모듈 비교가 목적이므로, 재생 후 자동 Destroy 코드는 넣지 않았습니다.
- 자동 Destroy/Instantiate 같은 런타임 C# 호출은 다음 DAY11에서 다루기 좋습니다.
