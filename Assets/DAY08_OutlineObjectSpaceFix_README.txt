DAY08 Outline Object Space FIX

문제:
- ToonCharacter와 ToonOutlineShell의 Local Position이 둘 다 (0,0,0)인데
  부모 03_OutlineShell을 Y로 움직이면 Shell만 한 번 더 이동해 보이는 현상.

원인:
- SG_OutlineShell의 Position 노드가 World Space로 남아
  World Position을 Object-space Vertex Position에 넣으면서 부모 Transform이 두 번 적용되는 현상.

수정:
- SG_OutlineShell.shadergraph에서
  Position Node Space = Object
  Normal Vector Node Space = Object
  로 직접 보정.

사용:
1) ZIP의 Assets 내용을 기존 프로젝트 Assets에 병합
2) Unity 컴파일 완료
3) Tools > DAY08 FIX > Fix Outline Object Space
4) 완료 후 ToonCharacter와 ToonOutlineShell Local Position을 둘 다 (0,0,0)
5) 부모 03_OutlineShell의 Y를 움직여 확인
6) 확인 후 Assets/Editor/DAY08_OutlineObjectSpaceFix.cs 삭제 가능

[v2] Unity 6.6에서 컴파일되지 않던 Regex.Replace 오버로드를 수정했습니다.
