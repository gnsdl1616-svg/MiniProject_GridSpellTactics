# Grid Spell Tactics

![Unity](https://img.shields.io/badge/Unity-2D-black?logo=unity)
![Genre](https://img.shields.io/badge/Genre-Roguelike%20%7C%20Deckbuilding%20%7C%20Turn--Based-blue)
![Status](https://img.shields.io/badge/Status-Portfolio%20Demo-orange)
![Language](https://img.shields.io/badge/Language-C%23-green)

> **Grid Spell Tactics**는 Unity 2D 기반의 로그라이크 / 덱빌딩 / 턴제 전투 포트폴리오 프로젝트입니다.  
> 플레이어는 방을 이동하며 전투에 진입하고, 카드 예약형 전투를 통해 적을 처치한 뒤 보상과 덱빌딩 흐름을 거쳐 다음 전투로 진행합니다.

---

## 📌 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 프로젝트명 | **Grid Spell Tactics** |
| 엔진 | Unity |
| 장르 | 2D Top-Down / Roguelike / Deckbuilding / Turn-Based |
| 핵심 구조 | 씬 전환 기반 런 진행 + 카드 예약형 전투 |
| 현재 목표 | 완성형 상용 게임이 아닌 **포트폴리오용 플레이 가능 데모 빌드** |
| 주요 키워드 | Grid Battle, Card Reservation, Roguelike Flow, Deck System |

---

## 🎮 핵심 게임 컨셉

플레이어는 직접 방을 이동하며 전투 / 보상 / 덱빌딩 흐름을 반복합니다.  
전투에서는 손패의 카드를 선택해 행동을 예약하고, 예약된 행동이 순차적으로 실행됩니다.

### 핵심 재미

- 적의 행동을 예측하고 안전한 위치를 선택하는 판단
- 카드를 어떤 순서로 예약할지 결정하는 전술성
- 이동 / 공격 / 방어가 모두 카드로 통합된 전투 구조
- 전투 후 보상과 덱 강화를 통한 로그라이크 성장 흐름

---

## 🧭 전체 씬 흐름

```text
Boot
→ Title
→ AdventureMap
→ Battle
→ Reward
→ Deckbuilding_Hub
→ AdventureMap
```

패배 흐름은 다음과 같습니다.

```text
Battle
→ Result
→ Title
```

---

## ⚔️ 전투 시스템

현재 전투는 **카드 예약 기반 Planning 전투**를 기준으로 구성되어 있습니다.

### 전투 흐름

```text
Planning
→ EnemyPlan
→ Execute
→ Resolve
```

| 단계 | 설명 |
|---|---|
| Planning | 플레이어가 손패에서 카드를 선택해 행동을 예약 |
| EnemyPlan | 적이 이번 턴의 행동을 예약하고 의도를 표시 |
| Execute | 플레이어와 적의 예약 행동을 순서대로 실행 |
| Resolve | HP, Block, 승리/패배, 카드 이동 상태를 정리 |

### 현재 구현 방향

- AP 제거
- Energy 단일 자원 사용
- 이동 / 공격 / 방어 / 대기 행동을 카드로 통합
- 1차 구현은 1:1 전투 기준
- 적은 고정 위치에 배치
- 플레이어는 Grid 위에서 이동
- 예약된 행동은 순차적으로 실행

---

## 🃏 카드 / 덱 시스템

카드 시스템은 다음 구조로 역할을 나누어 설계했습니다.

| 시스템 | 역할 |
|---|---|
| Card Library | 카드 정의 데이터 관리 |
| Battle Deck Runtime | Draw / Hand / Discard / Field 상태 관리 |
| Reserved Action | 예약된 카드 행동 저장 |
| Battle Execution Resolver | 예약 행동 실행 및 결과 계산 |

### 카드 흐름

```text
DrawPile
→ Hand
→ Reserved Action
→ Execute
→ DiscardPile / FieldPile
```

### 현재 기준 카드 예시

| 카드 | 역할 |
|---|---|
| Move 1 | 1칸 이동 |
| Dash 2 | 2칸 이동 |
| Attack 3 | 적에게 피해 |
| Guard 3 | Block 획득 |

---

## 🗺️ AdventureMap / 방 구조

AdventureMap은 방 이동과 전투 진입의 허브 역할을 합니다.

### 현재 가능한 흐름

- 방 구조 생성
- 방 타입 분류
- 방 프리팹 배치
- 문 기반 방 이동
- Combat 오브젝트 상호작용
- Battle 씬 진입

### 방 타입 예시

| 타입 | 설명 |
|---|---|
| Start | 시작 방 |
| Combat | 일반 전투 방 |
| Shop | 상점 방 |
| Event | 이벤트 방 |
| Boss | 보스 방 |

---

## 🖥️ 현재 구현된 주요 기능

### 공통 / 런 흐름

- `GameSceneManager` 기반 씬 전환
- `RunFlowController` 기반 씬 흐름 제어
- `RunStateService` 기반 런타임 데이터 저장
- Boot → Title → AdventureMap → Battle 흐름
- Battle 승리 / 패배에 따른 분기

### Battle

- 자동 Grid 생성
- 클릭 기반 이동 예약
- Enemy Zone 분리
- 플레이어 / 적 예약 행동 표시
- 적 Intent 표시
- Draw / Hand / Discard UI 표시
- Battle Log 표시
- Move / Attack / Hit / Guard / Death 연출 연결
- 전투 결과에 따른 Reward / Result 이동

### Reward / Deckbuilding

현재 빌드에서는 완성 기능이 아닌 **이미지 기반 플레이스홀더 화면**으로 구성되어 있습니다.

- Reward: 보상 시스템 목표를 보여주는 연결 화면
- Deckbuilding_Hub: 향후 덱 편집 시스템 목표를 보여주는 연결 화면
- 각 화면은 버튼을 통해 다음 씬으로 이동

---

## 📁 추천 폴더 구조

```text
Assets/
 ┣ Scripts/
 ┃ ┣ Core/
 ┃ ┣ RuntimeData/
 ┃ ┣ AdventureMap/
 ┃ ┣ Battle/
 ┃ ┣ Card/
 ┃ ┣ UI/
 ┃ ┗ Common/
 ┣ Scenes/
 ┣ Prefabs/
 ┣ Sprites/
 ┣ Animations/
 ┣ Audio/
 ┗ Materials/
```

---

## 🧪 현재 빌드 검증 흐름

### 승리 루트

```text
Boot
→ Title
→ AdventureMap
→ Battle
→ Reward
→ Deckbuilding_Hub
→ AdventureMap
```

### 패배 루트

```text
Battle
→ Result
→ Title
```

### 최종 빌드 전 체크리스트

- [ ] Boot에서 Title로 정상 이동
- [ ] Title에서 AdventureMap으로 정상 이동
- [ ] AdventureMap에서 상호작용으로 Battle 진입
- [ ] Battle에서 카드 예약 / 실행 가능
- [ ] 승리 시 Reward 이동
- [ ] 패배 시 Result 이동
- [ ] Reward 버튼으로 Deckbuilding_Hub 이동
- [ ] Deckbuilding_Hub 버튼으로 AdventureMap 복귀
- [ ] Result 버튼으로 Title 복귀
- [ ] Quit 버튼 동작 확인

---

## 🚧 현재 한계점

- Reward는 실제 카드 선택 시스템이 아닌 플레이스홀더입니다.
- Deckbuilding_Hub는 실제 덱 편집 시스템이 아닌 구현 목표 화면입니다.
- 다수 적 전투는 아직 확장 전입니다.
- 상점 / 이벤트 / 보스 콘텐츠는 기본 구조만 계획되어 있습니다.
- 카드 밸런스와 연출 폴리싱은 추가 작업이 필요합니다.

---

## 🔮 향후 개선 계획

- 실제 보상 선택 시스템 구현
- 덱 편집 / 카드 제거 / 강화 기능 구현
- 다수 적 전투 확장
- 상태이상 / 범위 공격 / 특수 카드 추가
- Shop / Event / Boss 방 콘텐츠 연결
- 카드 데이터 외부화
- 전투 연출 및 사운드 개선
- 저장 / 해금 / 영구 재화 시스템 확장

---

## 🧩 주요 스크립트 예시

| 분류 | 스크립트 |
|---|---|
| Scene Flow | `GameSceneManager`, `RunFlowController`, `RunStateService` |
| Entry Point | `BootSceneEntryPoint`, `TitleSceneEntryPoint`, `BattleSceneEntryPoint` |
| Adventure | `AdventureMapSceneEntryPoint`, `AdventureCombatInteractable` |
| Battle | `BattleFlowController`, `BattleRuntimeState`, `BattleExecutionResolver` |
| Grid | `BattleGridAutoBuilder`, `BattleGridViewBinder` |
| Card | `BattleCardDefinition`, `BattleCardLibrary`, `BattleDeckRuntimeState` |
| UI | `BattleUIView`, `CommonSceneButtonHandler` |

---

## 🛠️ 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity |
| Language | C# |
| Platform | PC Build 기준 |
| Version Control | Git / GitHub |

---

## 📷 스크린샷

> 아래 이미지는 GitHub 업로드 후 `Images` 또는 `Docs` 폴더에 이미지를 추가한 뒤 경로를 수정해서 사용하면 됩니다.

```md
![Battle Screenshot](Docs/Images/battle_screen.png)
![AdventureMap Screenshot](Docs/Images/adventure_map.png)
```

---

## 📌 프로젝트 상태 요약

이 프로젝트는 완성된 상용 게임이 아니라,  
**로그라이크 덱빌딩 전투 게임의 핵심 구조와 플레이 흐름을 검증하기 위한 포트폴리오 데모 빌드**입니다.

현재 빌드의 핵심 가치는 다음과 같습니다.

- 전체 씬 흐름이 끊기지 않음
- AdventureMap에서 Battle로 진입 가능
- 카드 예약형 전투 루프가 시각적으로 동작
- 승리 / 패배 결과에 따라 씬이 분기됨
- Reward / Deckbuilding 흐름이 플레이스홀더로 연결됨

---

## 👤 제작자

- Unity 2D 개인 프로젝트
- Portfolio Demo Project
