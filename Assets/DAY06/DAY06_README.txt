DAY 06 재완성팩 - 정점 변형 + UV 애니메이션

이번 수정 내용
- WaterSurface_Albedo.png를 사용해 최종 물 표면 무늬 적용
- VertexWave_TestGrid.png 포함 및 테스트 전환 메뉴 제공
- WaterSurface_Albedo: Wrap Mode Repeat / Filter Mode Bilinear 자동 설정
- VertexWave_TestGrid: Wrap Mode Repeat / Filter Mode Point 자동 설정
- 물 텍스처가 단색처럼 보이던 임의 WaterTint 제거
- 기존 DAY06_VertexWave 씬/Mat_VertexWave가 있어도 새 리소스로 다시 적용
- 자동 임포트 시 현재 작업 중인 다른 씬으로 강제 전환하지 않음

DAY06 문서 기준 기본값
- Amplitude = 0.15
- WaveFrequency = 2
- WaveSpeed = 1.5
- UvTiling = (1, 1)
- UvFlowDirection = (0.03, 0.08)
- UvFlowSpeed = 0.2
- CrossWaveFrequency = 1.6
- CrossWaveSpeed = 1.1
- CrossWaveStrength = 0.5

적용 방법
1. 이 ZIP의 Assets 내용을 기존 GameGraphics/Assets에 병합/덮어쓰기합니다.
2. Unity에서 파일 변경 창이 뜨면 Reload를 누릅니다.
3. Import가 끝나면 Tools > DAY06 > Finalize Vertex Wave Demo를 한 번 누릅니다.
4. Assets/Scenes/DAY06_VertexWave 씬을 열고 Play합니다.
5. 최종 물 무늬: Tools > DAY06 > Use Water Texture
6. 격자 테스트: Tools > DAY06 > Use Test Grid Texture

정상 결과
- Water Texture: 링크에서 본 밝은 하늘색 물결/카우스틱 무늬가 표면에 보이며 천천히 흐릅니다.
- Vertex Wave: Plane 메시 자체가 Y축으로 부드럽게 출렁입니다.
- Test Grid: 격자/점이 함께 휘어 정점 변형 여부를 확인할 수 있습니다.
