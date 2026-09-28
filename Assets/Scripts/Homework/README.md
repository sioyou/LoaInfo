# LoaInfo · 로아 숙제 트래커 (Unity)

다크모드 숙제 현황 앱입니다. Play만 누르면 바로 실행됩니다.

## 사용법

1. Unity에서 이 프로젝트를 연다 (6000.3.x)
2. `SampleScene` Play
3. **API 키** 버튼 → [로스트아크 OpenAPI](https://developer-lostark.game.onstove.com/) 에서 발급한 키 저장
4. **+ 캐릭터** → 닉네임 검색 → 원정대에서 숙제 볼 캐릭터 체크 → 추가
5. 캐릭터 카드에서 일일/주간 숙제 체크

## 숙제 규칙

| 항목 | 설명 |
|------|------|
| 가토 / 균열 | 각각 탭해서 완료. 휴게는 **1칸=1.0**, 미완료 시 +0.5칸(반칸), **최대 5칸**. 게이지 탭으로 수정. 06시 리셋 |
| 할의 모래시계 | 템렙 **1730 이상** 캐릭터에 자동 표시. 주 1회, 탭 완료. **수요일 오전 6시** 초기화 |
| 주간 레이드 | **4막 · 종막 · 세르카 · 성당** (+싱글). 난이도 1개만 선택. 완료/선택은 청보라. **수요일 06시** 초기화 |

캐릭터 추가 시 추천 레이드가 자동 등록되며, `[+ 레이드]`로 더 넣거나 뺄 수 있습니다.

> 레이드 목록은 `RaidCatalog.cs`에 고정되어 있습니다.

## 저장 위치

PlayerPrefs (`LoaInfo.Homework.Save`)
- API 키, 캐릭터 목록, 일일/주간 숙제·휴게 상태를 JSON으로 저장
- 변경 시마다 `PlayerPrefs.Save()`로 즉시 반영
- 앱 시작 시 자동 복원

예전 파일 저장(`persistentDataPath/loa_homework.json`)이 있으면 첫 실행 때 PlayerPrefs로 자동 이전합니다.

## UI / 폰트

- 텍스트는 **TextMeshPro** + **Maplestory Light**
- 프리팹: `Assets/Prefabs/HomeworkUI/`
- 카탈로그: `Assets/Resources/HomeworkUI/UiCatalog.asset`
- Unity 메뉴 **LoaInfo → Build Homework UI Prefabs** 로 폰트 에셋/프리팹 재생성 가능 (프로젝트 열면 없으면 자동 생성)
