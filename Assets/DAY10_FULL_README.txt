DAY10 FULL - Particle System 모듈과 프리팹화 전체 재구축

실행
1) ZIP의 Assets 내용을 기존 GameGraphics 프로젝트 Assets에 병합
2) Unity 컴파일 완료 대기
3) Tools > DAY10 FULL > Build Complete DAY10
4) Assets/DAY10/Scenes/DAY10_ParticlePrefabs_COMPLETE.unity 확인

생성
Assets/DAY10/Prefabs/Effects/
- FX_HitSpark.prefab
- FX_HealGlow.prefab
- FX_ExplosionSmall.prefab

Assets/DAY10/Materials/
- Mat_HitSpark.mat
- Mat_HealGlow.mat
- Mat_ExplosionSmall.mat

과정까지 포함
1. DAY09 FX_HitSpark_Test를 출발점으로 FX_HitSpark Prefab 생성
2. HitSpark 복제 → HealGlow
   - 초록색
   - 천천히 상승
   - Emission / Color / Lifetime / Renderer Material 변경
3. HitSpark 복제 → ExplosionSmall
   - Burst
   - 큰 Size 변화
   - Color / Lifetime / Renderer Material 변경
4. 같은 HitSpark Prefab 두 위치에 재사용
5. 한 HitSpark 인스턴스만 Start Size Override
6. Prefab 체크리스트
   - Looping
   - Play On Awake
   - Stop Action
   - Material
   - Scale (1,1,1)
   - Override
7. Tools > DAY10 FULL > Validate DAY10 Prefabs 로 자동 검증

Prefab Mode
- Project > DAY10 > Prefabs > Effects 에서 프리팹 더블클릭
- 또는 Tools > DAY10 FULL > Open ... Prefab Mode

종료 처리
- 세 Prefab 모두 1회성
- Main > Stop Action = Destroy
- 재생 종료 후 런타임 인스턴스가 스스로 제거됨

크기 규칙
- Transform Scale은 (1,1,1)
- Start Size / Size over Lifetime로 조절

완료 후
- Assets/Editor/DAY10_FULL_AutoSetup.cs는 삭제 가능
- DAY10 Prefabs / Materials / Scene은 유지


[v2 목적별 동작 수정]

세 이펙트를 같은 규칙으로 통일하지 않습니다.

FX_HitSpark
- 목적: 공격 적중
- Looping: OFF
- Stop Action: Destroy
- 이유: 실제 피격 1회에 딱 한 번 재생되는 신호

FX_HealGlow
- 목적: 지속 회복 / 회복 오라 예제
- Looping: ON
- Stop Action: None
- Emission: 낮은 Rate over Time + 시작 Burst
- 이유: 회복 상태가 유지되는 동안 초록 입자가 천천히 계속 상승
- 종료: 회복 상태가 끝나는 게임 코드에서 Stop/Disable하는 방식

FX_ExplosionSmall
- 목적: 작은 폭발
- Looping: OFF
- Stop Action: Destroy
- 이유: 폭발 사건 1회에 한 번만 터지고 종료

검증 메뉴도 이제 각 Prefab의 목적별 기대값을 따로 검사합니다.


[v3 지속시간 조정]
- FX_HitSpark: Start Lifetime 0.35~0.55초
  - Loop OFF 유지
  - 한 번의 피격 신호는 그대로, 눈으로 읽기 쉽게만 연장
- FX_ExplosionSmall: Start Lifetime 0.50~0.80초
  - Loop OFF 유지
  - 한 번 폭발 후 조금 더 잔상이 남음
- FX_HealGlow: Start Lifetime 1.20~1.80초
  - Loop ON 유지
  - 초록 입자가 더 오래 남아 회복 오라 흐름을 확인하기 쉬움

두 번 깜빡이는 방식은 사용하지 않습니다.
'한 사건 = 한 번 재생'이라는 의미를 유지하고, 각 입자의 수명만 늘렸습니다.
