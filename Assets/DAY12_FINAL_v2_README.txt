DAY12 FINAL v2 - Visual Effect Graph 입문
=============================================

이번 버전은 이전처럼 '램프 아이콘 + 빈 그래프'에서 끝나지 않습니다.
실제 VFX Graph 내부 Context/Block을 생성해 최종 GPU Spark가 재생되도록 만드는 버전입니다.

전제
- Unity 6.x
- URP
- Visual Effect Graph 패키지 설치됨
- 이전 DAY12에서 램프 아이콘이 보였다면 패키지는 이미 설치된 상태입니다.

적용
1. 기존 Assets/DAY12/EditorInternal 폴더가 있으면 삭제
2. 이 ZIP의 Assets 폴더를 기존 프로젝트 Assets에 병합
3. Unity 컴파일 완료 대기
4. Tools > DAY12 FULL > FINAL - Rebuild GPU Spark Complete
5. Assets/DAY12/Scenes/DAY12_VFXGraphBasics_COMPLETE.unity
6. Play
7. Game View / Scene View에서 주황색 GPU Spark 확인
8. Assets/DAY12/VFX/VFX_GpuSpark.vfx 더블클릭

실제 Graph에 들어가는 과정
Spawn
- Constant Spawn Rate = 80

Initialize Particle
- Set Lifetime Random = 0.4 ~ 1.2
- Set Position Shape = Sphere
- Set Velocity Random = 위쪽/바깥쪽
- Set Size Random = 0.03 ~ 0.12

Update Particle
- Add Force 계열
- Drag
- Age/Lifetime 기반 Size 변화(패키지 블록이 지원되는 경우 Attribute Curve로 구성)

Output Particle
- Quad
- Orange Color
- Alpha = 1 (해당 패키지에서 독립 Alpha attribute가 지원될 때)

최종 씬
- VFX_GpuSpark_Player
- Visual Effect Component
- Visual Effect Asset = VFX_GpuSpark
- 카메라를 가까이 배치하고 어두운 배경으로 Spark가 잘 보이게 구성

검증
Tools > DAY12 FULL > Validate DAY12 Complete

그래프 직접 열기
Tools > DAY12 FULL > Open VFX_GpuSpark Graph

왜 asmref가 있나?
VFX Graph는 Context/Block 생성용 Editor API가 public API가 아닙니다.
Unity의 Visual Effect Graph Editor assembly 안에서 실제 Graph Model을 수정하기 위해
DAY12_VFXBuilder.asmref가 Unity.VisualEffectGraph.Editor를 참조합니다.

완료 후 삭제 가능
Assets/DAY12/EditorInternal/
- DAY12_VFXBuilder.cs
- DAY12_VFXBuilder.asmref

남겨야 함
Assets/DAY12/VFX/VFX_GpuSpark.vfx
Assets/DAY12/Scenes/DAY12_VFXGraphBasics_COMPLETE.unity


[v3 FIX]
- CS0104 Transform ambiguity fixed by explicitly using UnityEngine.Transform.


[v4 FIX]
- Unity 6.6에서 제거/변경된 VisualEffectResource.GetOrCreateGraph 직접 호출 제거.
- VFXGraph를 Property/Field/Method Reflection으로 찾는 호환 코드로 변경.


[v5 FIX]
- Unity 6.6에서 VFXBlock.label이 직접 노출되지 않는 CS1061 수정.
- label/title/name은 Reflection 호환 함수로 읽고 쓰도록 변경.


[v6 FIX]
- 'VFXGraph를 가져오지 못했습니다' 런타임 오류 수정.
- Unity VFX Graph Editor가 실제로 사용하는 VisualEffectResource.GetResourceAtPath(path).GetGraph() 경로로 변경.
- 저장도 resource.WriteAssetWithSubAssets() 경로로 변경.
