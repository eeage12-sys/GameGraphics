DAY12 FINAL - Visual Effect Graph GPU Spark

이 버전은 이전 DAY12 v1~v8의 교체본입니다.

수정 핵심
- Scalar Random: PerComponent가 아니라 Uniform 사용
- Lifetime Random: 0.4 ~ 1.2
- Size Random: 0.03 ~ 0.12
- Velocity Random: PerComponent, 위쪽/바깥쪽
- Color: VFX Color 슬롯 타입에 맞는 Vector3 주황색
- Age over Lifetime: 실제 AttributeFromCurve / OverLife / Size 사용
- Spawn Rate: 80
- Output Particle Quad
- VFX Graph 창: Unity VFXViewWindow.GetWindow + LoadAsset 방식으로 실제 에셋 로드
- No Asset 탭 문제 수정

적용
1. 기존 Assets/DAY12/EditorInternal 폴더 삭제
2. 이 ZIP의 Assets를 프로젝트 Assets에 병합
3. Unity 컴파일 완료
4. Tools > DAY12 FINAL > Rebuild Complete
5. DAY12_VFXGraphBasics_COMPLETE 씬 열기
6. Play
7. 작은 주황색 Spark가 계속 생성되는지 확인
8. Tools > DAY12 FINAL > Open VFX_GpuSpark Graph
9. 그래프에서 Spawn -> Initialize -> Update -> Output 확인
10. Tools > DAY12 FINAL > Validate

완료 후
- Assets/DAY12/EditorInternal 삭제 가능
- Assets/DAY12/VFX 및 Scenes 유지


[v10 FIX - current screenshot]
- CS0103: AttributeCompositionMode does not exist in the current context
- Cause: AttributeCompositionMode belongs to UnityEditor.VFX.Block namespace.
- Fix: every reference changed to Block.AttributeCompositionMode.
- Also rechecked the source for prior failures:
  * no GetOrCreateGraph
  * no direct VFXBlock.label assignment
  * no non-generic VFXSlotContainerModel parameter
  * graph open uses VFXViewWindow.GetWindow(asset, true) + LoadAsset(asset, null)
