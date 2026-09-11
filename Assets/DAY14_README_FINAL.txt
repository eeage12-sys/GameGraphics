DAY14 FINAL - 마법 훈련장 / 마력 제어 실험실
=============================================

이번 버전의 목표
- 단순 DAY 샘플 전시장이 아니라 '하나의 작은 게임 장면'으로 보이게 재구성.
- DAY14 문서의 권장 주제 '마법 훈련장'을 실제 배치/역할/색으로 드러냄.
- 새 Shader Graph를 무작정 만들지 않고 DAY05 SG_Shield를 복제해 사용.
- DAY10 / DAY11 / DAY12 / DAY13 결과를 역할에 맞게 통합.

장면 구성
- 중앙: 보호막 훈련 대상
  * SG_Portfolio_MagicShield
  * Fresnel + Emission
- 왼쪽: 회복 구역
  * FX_HealGlow
- 오른쪽: 타격 시험 구역
  * Impact Pad
  * Ground Click -> FX_HitSpark
- 뒤쪽: 마력 분수 장치
  * VFX_GpuSpark
  * 1 = SpawnRate 20
  * 2 = SpawnRate 200

아트 방향
- Floor/Wall: 어두운 청회색
- Accent: 청록 Emission
- Hit/VFX: 주황
- 기존 GraphicsLab 오브젝트는 LegacyReference_Disabled에 보존

사용
1. ZIP 안 Assets를 프로젝트 Assets에 병합
2. Unity 컴파일 완료
3. Tools > DAY14 MAGIC TRAINING > 1 - Build Final Scene
4. Assets/DAY14/Scenes/GraphicsPortfolio_MagicTrainingGround.unity 열기
5. Play
6. 오른쪽 Impact Pad 좌클릭
7. 숫자 1 / 2 비교
8. Tools > DAY14 MAGIC TRAINING > 2 - Validate Final Scene

문서
- Assets/DAY14/Documentation/DAY14_Verification_Record.md
- Assets/DAY14/Documentation/DAY14_Spatial_Placement.md

최종 확인 후 삭제 가능
- Assets/Editor/DAY14_MagicTrainingGroundBuilder.cs

남겨야 함
- Assets/DAY14/Scripts/DAY14MagicTrainingHUD.cs
- Assets/DAY14/Scenes
- Assets/DAY14/Shaders
- Assets/DAY14/Materials
- Assets/DAY14/Settings
- Assets/DAY14/Documentation
