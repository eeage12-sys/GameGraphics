DAY08 FINAL v9 - Unity 6.6 / URP

이번 버전은 DAY08을 한 번에 전체 재구축합니다.

실행:
1. 기존 Assets/Editor/DAY08_FULL_ShaderGraphBuilder.cs를 삭제
2. 이 ZIP의 Assets 내용을 프로젝트 Assets에 덮어쓰기
3. Unity 컴파일 완료 대기
4. Tools > DAY08 FULL > FINAL - Rebuild Complete DAY08 실행

생성/수정:
- 실제 Shader Graph 4개
  SG_ToonBand
  SG_ToonRim
  SG_OutlineShell
  SG_ScreenOutline
- Material 전체
- Prefab 전체
- OutlineTarget Layer
- Render Objects Renderer Feature
- Full Screen Pass Renderer Feature (Color + Normal + Depth)
- 단계별 Scene 01~09
- DAY08_NonPhotoreal_COMPLETE
- Volume Color Grading
- TF2 / Guilty Gear / Hi-Fi RUSH 비교용 구성

중요:
- Outline Shell은 Opaque + Render Face Back을 Shader Graph 파일에도 강제로 보정합니다.
- Screen Outline은 Color/Normal/Depth를 요청하고 BlitSource 위에 선만 덮습니다.
- Screen Outline은 Renderer-global이므로 COMPLETE 씬에서는 꺼져 있습니다.
  실제 화면 외곽선 결과는 DAY08_05_ScreenOutline 또는 DAY08_08_HiFi 씬을 여세요.
- DAY08_RenderFeatureState.cs는 각 Scene에서 Renderer Feature ON/OFF를 자동으로 맞추므로 삭제하지 마세요.
- 모든 확인이 끝나면 Assets/Editor/DAY08_FULL_ShaderGraphBuilder.cs만 삭제해도 됩니다.
