# 전우치 타일맵 시작하기

Unity 6000.3.22f1 / 2D URP 프로젝트용 초기 지형 작업 공간입니다.

1. `Woochi > Tilemap > Open Starter Scene`을 선택합니다.
2. Tile Palette 창에서 `WoochiStarter` 팔레트를 선택합니다.
3. Active Tilemap을 `Ground`로 지정합니다. 팔레트 왼쪽 청록색 타일을 선택하고 Scene 뷰에 칠합니다.
4. 지우개로 지형을 지울 수 있습니다. 작업 후 씬을 저장합니다.

## 구성

- `Scenes/TilemapSandbox.unity`: 바닥, 벽, 높이가 다른 발판 세 개와 직교 카메라.
- `Grid/Background`: Order -10, 충돌 없는 배경. 팔레트 가운데 어두운 타일 사용.
- `Grid/Ground`: Order 0, 고정 Rigidbody2D + TilemapCollider2D + CompositeCollider2D (Merge).
- `Grid/Foreground`: Order 10, 충돌 없는 전경. 팔레트 오른쪽 황금색 타일 사용.
- `PlayerSpawn`: 이후 플레이어를 배치할 시작 위치. 플레이어 컨트롤러는 아직 없습니다.
- `Art/PrototypeTile.png`: 32×32 테스트 이미지, 32 PPU, Point 필터, 압축·밉맵 없음.
- `Tiles/Palettes/WoochiStarter.prefab`: 세 타일이 담긴 편집용 팔레트.

격자 한 칸은 1 Unity unit입니다. Ground 타일만 충돌 형상을 가집니다.
각 Tilemap Renderer는 Chunk 모드이며, 타일마다 GameObject를 생성하지 않습니다.
Game 뷰를 16:9로 보면 카메라에 가로 약 32칸, 세로 18칸이 표시됩니다.
Unlit 재질을 사용해 조명이 없어도 테스트 지형이 보입니다.

`Create Starter Assets`는 기존 에셋을 재사용하고 기존 테스트 씬은 덮어쓰지 않습니다.
팔레트/씬은 실제 저장된 에셋이므로 메뉴 스크립트 없이도 일반 Tilemap처럼 편집할 수 있습니다.
기존 SampleScene과 Build Profiles의 씬 목록은 유지합니다. 빌드할 때는 원하는 씬을 목록에 추가하세요.
