DAY01~DAY10 전체 정리용 1회성 Tool

목표:
- Assets 루트에 DAY01 ~ DAY10 폴더 생성
- 각 DAY에 해당하는 Scene / Material / Shader / Texture / Prefab / Settings를 해당 DAY 폴더로 이동
- AssetDatabase.MoveAsset을 사용하므로 GUID 참조 유지
- 공용 Assets/Settings(URP/Renderer), TutorialInfo는 건드리지 않음
- DAY02는 DAY01의 GraphicsLab을 이어서 사용하므로 DAY02_GraphicsLab_PBR 씬 복사본 생성
- 이전 자동화 폴더가 비면 자동 제거
- 정리 성공 후 이 Tool 스크립트는 자동 삭제

사용:
1. 이 ZIP의 Assets 폴더를 GameGraphics 프로젝트 루트에 병합
2. Unity가 컴파일할 때까지 기다림
3. Tools > PROJECT CLEANUP > Organize DAY01-DAY10 실행
4. 완료 팝업 확인
5. Project 창에서 Assets/DAY01 ~ DAY10 확인

주의:
- 삭제보다는 이동 위주로 동작함
- 이름이 겹치는 구버전 자산은 _Old1, _Old2 형식으로 보존함
- SampleScene, URP Settings, TutorialInfo 같은 공용/템플릿 자산은 자동 삭제하지 않음
