DAY08 재구축 패키지

목적
- 기존 DAY08이 한 개 Capsule 위주라 비교가 어려웠던 문제를 보완합니다.
- Tools는 딱 한 번 생성용으로만 사용하고, 생성 후 AutoSetup 스크립트를 삭제해도 결과물은 유지됩니다.

적용
1. ZIP 압축 해제
2. 안의 Assets 폴더를 기존 GameGraphics 프로젝트 루트에 병합
3. Unity Import/Compile 완료 대기
4. Tools > DAY08 REBUILD > Build Complete Core Demo 실행
5. Assets/Scenes/DAY08_NonPhotoreal_Full.unity 확인

생성되는 비교 장면
- 01 Toon Band / Light Left
- 02 Toon Band / Default Direction
- 03 Toon Band / Light Right
- 04 Toon + Rim Light
- 05 Toon + Rim + Outline Shell
- 06 TF2 Inspired (Outline 없음)
- 07 Guilty Gear Inspired (Inverted Hull Outline)

생성 Materials
Assets/GameGraphics/Day08_Rebuild/Materials/
- Mat_ToonBand_Left
- Mat_ToonBand_Default
- Mat_ToonBand_Right
- Mat_ToonRim
- Mat_OutlineShell
- Mat_TF2
- Mat_GuiltyGear

정리
생성이 끝난 뒤 Project 창에서
Assets/Editor/DAY08_REBUILD_AutoSetup.cs
를 삭제하면 Tools > DAY08 REBUILD 메뉴가 사라집니다.
씬/머티리얼/셰이더는 그대로 남습니다.

중요
- 이 패키지는 DAY08 핵심 결과(Toon Band / Rim / Inverted Hull Outline)와 스타일 비교를 명확하게 보이도록 재구축한 것입니다.
- Shader Graph 노드 Asset 자체가 아니라 URP HLSL Shader로 같은 핵심 계산을 구현합니다.
- 문서의 Screen Space Outline은 '심화' 파트이며, 이 패키지에는 포함하지 않았습니다. Renderer Feature까지 요구하는 수업 제출이라면 별도 구현이 필요합니다.
