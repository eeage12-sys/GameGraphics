DAY 07 완료용 오버레이

목표
- Shader Graph의 Blackboard / Vertex Position / Base Color가 HLSL의 Properties / vert / frag와 어떻게 대응하는지 확인
- 가장 단순한 URP Unlit HLSL 셰이더를 Cube에 적용
- Material의 Base Color를 바꾸면 Cube 색이 바뀌는지 확인

포함 파일
- Assets/Day07UnlitColor.shader
- Assets/Editor/DAY07_AutoSetup.cs

적용 방법
1. 이 ZIP의 Assets 폴더 내용을 기존 GameGraphics/Assets에 병합합니다.
2. Unity가 Import를 끝낼 때까지 기다립니다.
3. Tools > DAY07 > Finalize HLSL Demo 실행
4. Assets/Scenes/DAY07_HLSL.unity가 생성되고 열립니다.
5. DAY07_ColorCube를 선택하고 Materials의 Mat_Day07UnlitColor를 확인합니다.
6. Material Inspector의 Base Color를 바꾸거나 Tools > DAY07 > Set Cube Red/Green/Blue를 눌러 결과를 확인합니다.

코드에서 볼 핵심
- Properties의 _BaseColor = Material Inspector 값
- Attributes.positionOS : POSITION = Mesh 정점 위치 입력
- vert() + TransformObjectToHClip = 정점을 화면 위치로 변환
- Varyings.positionCS : SV_POSITION = 화면 배치용 위치 전달
- frag() : SV_Target = 최종 픽셀 색 출력
- return _BaseColor = Material의 색을 그대로 화면에 출력

주의
- 이 DAY07 파일은 기존 SG_ColorPulse, SG_Shield, DAY06 파일을 삭제하거나 교체하지 않습니다.
- 별도 DAY07_HLSL 씬을 생성하므로 이전 씬들은 Assets/Scenes에 그대로 남습니다.
