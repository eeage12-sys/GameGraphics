DAY 08 자동 구성팩

핵심 실습
1) Toon Band: 밝은 면/어두운 면 2단계 분리
2) Rim Light: 카메라 가장자리에 청록색 Rim
3) Outline Shell: Inverted Hull 방식 외곽선

설치
- 이 ZIP의 Assets 폴더를 기존 GameGraphics 프로젝트에 병합합니다.
- Unity Import/Compile이 끝난 뒤 Tools > DAY08 > Finalize Toon Rim Outline Demo를 누릅니다.
- Assets/Scenes/DAY08_NonPhotoreal.unity 씬이 만들어집니다.

확인 메뉴
- Tools > DAY08 > Show Toon Band Only
- Tools > DAY08 > Show Toon Rim
- Tools > DAY08 > Show Toon Rim + Outline
- Tools > DAY08 > Style - TF2 Inspired
- Tools > DAY08 > Style - Guilty Gear Inspired

주의
- DAY08 문서의 핵심 3개(Toon Band, Rim Light, Outline Shell)를 안정적으로 확인하도록 HLSL/ShaderLab으로 구현했습니다.
- 문서의 Screen Outline은 '심화'이며 URP Renderer Feature 자산을 수정해야 하므로 이 자동팩에서는 제외했습니다.
- 수업에서 Shader Graph 노드 자체 제출을 요구하면 Graph 자산은 별도로 만들어야 합니다.
