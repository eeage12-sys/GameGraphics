DAY 05 완료본

선택 실습: 마법 보호막

완성 파일
- Assets/SG_Shield.shadergraph
- Assets/Materials/Mat_Shield.mat  (프로젝트 첫 실행 시 자동 생성)
- Assets/Scenes/DAY05_Shield.unity
- Assets/Editor/DAY05_AutoSetup.cs

보호막 구성
- Surface Type: Transparent
- Blend Mode: Alpha
- ShieldColor -> Base Color
- RimPower -> Fresnel Effect Power
- Fresnel x ShieldColor x EmissionStrength x Pulse -> Emission
- Fresnel x AlphaStrength -> Alpha
- Pulse: Time x PulseSpeed -> Sine -> Remap(0.5~1.0)

기본값
- ShieldColor: 하늘색
- RimPower: 3
- EmissionStrength: 2
- AlphaStrength: 0.45
- PulseSpeed: 2

프로젝트를 열면 Editor 스크립트가 Mat_Shield를 만들고
DAY05_Shield.unity의 Sphere (3)을 DAY05_ShieldDemo로 바꾼 뒤 머티리얼을 자동 적용합니다.
필요하면 Tools > DAY05 > Finalize Shield Demo를 눌러 다시 실행할 수 있습니다.
