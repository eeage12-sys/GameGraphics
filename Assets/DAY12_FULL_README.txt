DAY12 FULL - Visual Effect Graph 입문

이 ZIP은 DAY12를 Particle System이나 임의 코드 이펙트로 우회하지 않습니다.
Unity의 실제 Visual Effect Graph 패키지와 실제 .vfx 에셋을 사용합니다.

실행 순서
1) ZIP 안의 Assets 내용을 GameGraphics 프로젝트 Assets에 병합
2) Unity 컴파일 완료
3) Tools > DAY12 FULL > 1 - Install or Check VFX Graph
4) 패키지 설치/확인 완료
5) Tools > DAY12 FULL > 2 - Build DAY12
6) Tools > DAY12 FULL > 3 - Open VFX_GpuSpark Graph
7) Assets/DAY12/VFX/DAY12_GpuSpark_GraphChecklist.txt 순서대로 실제 Context/Block 확인 및 설정
8) Graph 저장
9) DAY12_VFXGraphBasics_COMPLETE 씬 열기
10) Play Mode 확인
11) Tools > DAY12 FULL > 4 - Validate DAY12

Build가 만드는 것
Assets/DAY12/VFX/VFX_GpuSpark.vfx
- Unity Visual Effect Graph 패키지의 실제 Minimal System 템플릿을 기반으로 생성
- Spawn > Initialize > Update > Output 기본 Context가 있는 진짜 VFX Graph

Assets/DAY12/Scenes/DAY12_VFXGraphBasics_COMPLETE.unity
- VFX_GpuSpark_Player
- Visual Effect 컴포넌트
- VFX_GpuSpark 에셋 연결
- Main Camera
- Ground Reference
- 수업 단계별 Hierarchy 체크 오브젝트

수업 기본값
Spawn Rate: 80
비교: 20 / 200
Lifetime Random: 0.4 ~ 1.2
Position Shape: Sphere 또는 Circle (기본 선택 Sphere)
Velocity: 위쪽 또는 바깥 방향
Size Random: 0.03 ~ 0.12
Update: Add Force / Drag / Age over Lifetime
Output: Quad
Color: 주황색 또는 하늘색 (기본 선택 주황색)
Blend: 밝은 효과라면 Additive 계열 고려
Texture: 선택 사항

왜 그래프 블록을 C#로 가짜 생성하지 않나?
DAY12 핵심이 VFX Graph의 Context와 Block을 직접 읽고 구성하는 것이기 때문입니다.
Unity VFX Graph의 내부 Editor API는 버전별 구현 차이가 큰 부분이라,
결과만 흉내 내는 Particle System/HLSL을 만들지 않고
Unity가 제공하는 실제 VFX Graph 템플릿을 만든 뒤 Graph 창에서 수업 블록을 직접 구성하도록 했습니다.

완료 후 삭제 가능
Assets/Editor/DAY12_FULL_AutoSetup.cs

남겨야 함
Assets/DAY12/VFX/VFX_GpuSpark.vfx
Assets/DAY12/VFX/DAY12_GpuSpark_GraphChecklist.txt
Assets/DAY12/Scenes/DAY12_VFXGraphBasics_COMPLETE.unity
