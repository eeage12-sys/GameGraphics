DAY08 FULL Shader Graph Builder v4 (Unity 6.x)

[v3 - Unity 6.6 optional-parameter Reflection compatibility fix]
DAY08 FULL - Shader Graph 전체 실습 자동 구성 (Unity 6 / URP)

목표
- DAY08 문서의 결과만 흉내내는 HLSL 우회본이 아니라, 실제 .shadergraph 자산을 생성/복제/수정합니다.
- Toon Band / Rim Light / Outline Shell / Screen Outline의 노드 그래프, 머티리얼, 비교 프리팹, 비교 씬, 스타일 프리셋, Volume까지 구성합니다.
- 기존 DAY04의 SG_ColorPulse.shadergraph를 '실제 Shader Graph 포맷/버전 템플릿'으로 사용합니다.
- Shader Graph 내부 API는 Unity 버전에 따라 달라질 수 있어 Unity 6.x/Shader Graph 17.x의 PropertyNode.property 바인딩을 우선 사용하고, 구버전 fallback도 포함합니다.

사용
1) ZIP의 Assets를 기존 GameGraphics 프로젝트에 병합
2) Unity Import/Compile 완료 대기
3) Tools > DAY08 FULL > Build ALL Shader Graph Coursework 실행
4) 완료 후 Assets/DAY08 확인
5) 생성 확인 후 Assets/Editor/DAY08_FULL_ShaderGraphBuilder.cs 삭제 가능

생성 목표
Assets/DAY08/
  Graphs/
    SG_ToonBand.shadergraph
    SG_ToonRim.shadergraph
    SG_OutlineShell.shadergraph
    SG_ScreenOutline.shadergraph
  Materials/
    Mat_ToonBand.mat
    Mat_ToonRim.mat
    Mat_OutlineShell.mat
    Mat_ScreenOutline.mat
    Mat_StyleCharacter_TF2.mat
    Mat_StyleCharacter_GuiltyGear.mat
    Mat_StyleCharacter_HiFi.mat
    Mat_StyleShell_GuiltyGear.mat
    Mat_StyleScreenOutline_HiFi.mat
  Prefabs/
    PF_ToonBand.prefab
    PF_ToonRim.prefab
    PF_OutlineDemo.prefab
    PF_Style_TF2.prefab
    PF_Style_GuiltyGear.prefab
    PF_Style_HiFi.prefab
  Scenes/
    DAY08_NonPhotoreal_COMPLETE.unity
  Settings/
    VP_DAY08_ColorGrading.asset

중요
- SG_ToonBand: Normal Vector(World) -> Dot Product -> Remap -> Step -> Lerp -> Base Color
- SG_ToonRim: 위 Toon Band + Fresnel -> Multiply -> Multiply -> Add -> Base Color
- SG_OutlineShell: Position(Object) + Normal(Object)*OutlineWidth -> Vertex Position, OutlineColor -> Base Color
- SG_ScreenOutline: Screen Position/Screen/Reciprocal/Combine/이웃 UV/Normal/Depth/Step/Maximum/Lerp 구조를 자동 생성 시도
- Renderer Feature도 자동 추가를 시도합니다.


v4 수정: PropertyNode를 Graph에 추가하기 전에 owner/propertyGuid를 먼저 설정하도록 변경. 슬롯 이름 차이에 대한 안전한 단일 슬롯 fallback 추가. Screen Outline UV의 좌/우/상/하 오프셋 구성도 실제 +/- 벡터 방식으로 수정.


[v6 변경]
- 사용자가 실제 Unity 6.6에서 만든 SG_ColorPulse / SG_Shield의 Shader Graph 포맷을 기준으로 슬롯 처리 방식을 보강했습니다.
- 이름 Reflection이 실패해도 표준 노드의 포트 순서로 연결합니다.
- DAY08의 ToonBand / ToonRim / OutlineShell은 Universal Unlit SubTarget으로 전환합니다.
- Screen Outline은 Universal Fullscreen SubTarget으로 전환합니다.
- Screen Outline의 Combine은 RG(Vector2)를 사용하고 음수 Offset은 Multiply(-1)로 생성합니다.
- Tools > DAY08 FULL > 00 - Check Unity 6.6 Templates 로 먼저 두 원본 Graph를 확인할 수 있습니다.

[v7 수정]
- Unity 6.6의 UniversalTarget에 새 SubTarget 인스턴스를 강제로 대입하던 방식을 제거했습니다.
- Unity 자체 생성 코드와 동일하게 TrySetActiveSubTarget(Type)을 호출합니다.
- SubTarget 전환 직후 ValidateGraph를 호출하지 않아 donor의 Master Stack Base Color / Position Block을 보존합니다.
- 최종 저장 시에만 ValidateGraph를 실행합니다.
