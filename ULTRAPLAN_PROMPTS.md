# ULTRAPLAN 프롬프트 모음 — 콘텐츠 레이어 구축

> Phase 1~12까지 완성된 "시스템"위에, 실제 게임 콘텐츠(캐릭터/몬스터/스킬/아이템/UI/퀘스트 등)를 디자이너·기획자가 SO·Inspector만으로 조립할 수 있도록 하는 **콘텐츠 레이어**를 Phase 13 이후로 설계·구현한다.
>
> 각 차수는 독립적인 `/ultraplan` 호출로 진행한다. 한 번에 전부 집어넣으면 플랜이 얕아지고 컨텍스트가 분산된다. **1차 완료 후 2차로 넘어가는 것을 권장**.

---

## 사전 준비 (공통)

- [ ] 모든 로컬 커밋을 `origin/main`에 push (클라우드 VM은 GitHub에서 clone한다)
- [ ] claude.ai 로그인 (Pro/Max/Team)
- [ ] Claude Code v2.1.91 이상
- [ ] `/tasks`로 기존 ultraplan 세션 정리

---

## 1차 — 표현 계층 (Presentation Hook System) ★ 최우선

### 목표
스킬·버프·이벤트 실행 시점에 **VFX / SFX / 카메라 / 애니메이션 / 타임라인 / 진동 / 카메라 셰이크** 등 "연출"을 디자이너가 SO만으로 주입·조립할 수 있는 범용 표현 계층을 만든다. 현재는 각 시스템(Gacha/Tutorial 등)이 개별 `IXxxPresentation`을 갖고 있어 표준이 없고, 스킬·버프에는 훅 지점 자체가 없다.

### 범위
- 포함: 표현 트랙 SO, 이벤트 페이로드(Caster/Target/Position/Timing) 표준, 재생 매니저, 풀링 연동, 타임라인/애니메이션 단계 동기화, 큐잉/중첩/취소 정책
- 제외: 실제 캐릭터/몬스터/스킬 데이터(2차 이후)

### 기존 시스템 연결
- `EventBus` — 표현 트리거는 이벤트 기반
- `ObjectPoolManager` — VFX/Prefab 풀 재활용
- `ISoundManager` — SFX 재생 위임
- `IGachaPresentation`, `ITutorialPresentation` — 기존 연출 인터페이스와 통일 또는 대체 방안 제시
- `BuffSystem.IBuffEffect.OnApply/OnTick/OnRemove` — 표현 훅 지점

### 프롬프트

```
/ultraplan PublicProject는 Unity 6 재사용 프레임워크(SYSTEMS.md 참고)이고 Phase 1~12까지 27개 시스템이 구현되어 있다. 지금은 스킬/버프/이벤트 실행 시 VFX/SFX/카메라/애니메이션/진동/셰이크 등 연출을 디자이너가 주입할 표준 "표현 계층"이 없다. 이를 해결하는 "Presentation Hook System"을 설계하라.

요구사항:
1. ScriptableObject 기반 "PresentationTrack"을 정의해 하나의 연출을 여러 단계(스폰/딜레이/크로스페이드/타임라인 동기화)로 구성
2. 단계 구성요소는 VFX Prefab(풀링), SFX, 카메라 셰이크, 카메라 연출, 애니메이션 트리거, 진동, 슬로우모션, 타임라인 클립
3. 이벤트 페이로드 표준(Caster, Target, Origin/Hit Position, Direction, Timing, Customs Dict)
4. 재생 매니저는 ServiceLocator 등록 + EventBus 구독
5. 풀링은 기존 ObjectPoolManager 재사용, 사운드는 ISoundManager 위임
6. 큐잉/중첩/취소 정책(즉시/대기/덮어쓰기/루트-서브 그룹)
7. 기존 IGachaPresentation, ITutorialPresentation과의 관계(통합/공존) 제안
8. BuffSystem.IBuffEffect와의 훅 지점(OnApply/OnTick/OnRemove → PresentationTrack 재생)
9. Inspector만으로 트랙을 조립 가능하게 하는 Attribute/Drawer 확장안(기존 27번 Attribute 패턴 준수)
10. CLAUDE.md 네이밍/SOLID/폴더 규칙 전면 준수(01_Interfaces/02_Core/03_Mono/04_Utils/09_ScriptableObjects)

결과물:
- 파일 단위 구현 계획(경로, 클래스명, 역할)
- 시스템 간 의존 다이어그램
- 사용 예시(스킬 "화염구" 시전 → 표현 트랙 재생 플로우)
- 다른 프로젝트에 복사 후 즉시 쓸 수 있는 재사용성 확인
- 테스트·검증 시나리오(로그 규칙 준수)
```

---

## 2차 — 스킬/어빌리티 시스템

### 목표
캐릭터·펫·몬스터가 공통으로 사용하는 **스킬 데이터 + 실행 파이프라인**. 대상 선택 → 비용 차감 → 쿨타임 → 효과 적용(데미지/힐/버프) → 표현 트랙 재생의 표준을 만든다.

### 선행 조건
- 1차(Presentation Hook System) 승인·병합 완료

### 범위
- 포함: SkillData SO, SkillInstance 런타임, 쿨타임/코스트/조건, 타깃팅 전략(Self/Ally/Enemy/Area/Chain), 효과 컴포저블(Damage/Heal/Buff/Status/Spawn), 표현 트랙 연결
- 제외: 캐릭터 데이터(3차), 전투 루프(5차)

### 프롬프트

```
/ultraplan PublicProject에 1차 Presentation Hook System이 병합된 상태다. 이 위에 캐릭터·펫·몬스터가 공통으로 사용하는 스킬/어빌리티 시스템을 설계하라.

요구사항:
1. ScriptableObject "SkillData" — 이름/설명(LocalizationKey)/아이콘/코스트/쿨타임/캐스트타임/사정거리/타깃팅/조건/효과리스트/표현트랙
2. 타깃팅 전략 인터페이스 ITargetingStrategy (Self, SingleAlly, SingleEnemy, AreaCircle, AreaLine, Chain 등). OCP로 확장 가능
3. 효과 컴포저블 ISkillEffect (Damage, Heal, BuffApply, BuffRemove, Teleport, Spawn, Custom). 하나의 스킬에 여러 개 조립 가능
4. 실행 파이프라인: 조건 검증 → 코스트 차감 → 쿨타임 시작 → 캐스트 대기 → 타깃 확정 → 효과 순차 적용 → PresentationTrack 발행(EventBus)
5. 기존 StatSystem(데미지 계산), BuffSystem(버프 적용), SaveSystem(쿨타임 영속화) 연동
6. SkillExecutor는 MonoBehaviour 독립 사용 가능 + EntityComponent로도 조립 가능하게 설계
7. 런타임 인스턴스 ISkillInstance (쿨타임 상태, 차지 수, 레벨)
8. 이벤트: SkillCastStarted/Casted/Cancelled/CooldownStarted/CooldownEnded/ResourceInsufficient
9. 기획자가 SO 하나로 스킬을 완성할 수 있어야 한다(코드 없이 새 스킬 추가)
10. CLAUDE.md 규칙 준수

결과물:
- 파일별 구현 계획
- 기존 StatSystem/BuffSystem/PresentationHook과의 연동 다이어그램
- 샘플 스킬 3종 SO 설정 예시(근접 단일, 원거리 광역, 지속 힐)
- 확장 체크리스트(새 효과 타입 추가 절차, 새 타깃팅 추가 절차)
```

---

## 3차 — 엔티티 시스템 (캐릭터 / 몬스터 / 펫)

### 선행 조건
- 2차 병합 완료

### 범위
- 포함: EntityData 공용 베이스 SO, CharacterData/MonsterData/PetData 파생, EntityComponent (MonoBehaviour), 스폰/디스폰, 리소스 로드, 상태 머신(Idle/Move/Cast/Hit/Dead)
- 제외: AI 로직 세부(필요 시 5차에서 확장), 팀 편성 UI(7차)

### 프롬프트

```
/ultraplan PublicProject에 1~2차(Presentation + Skill)가 병합되어 있다. 이제 캐릭터/몬스터/펫 엔티티 시스템을 설계하라.

요구사항:
1. ScriptableObject 계층: EntityData(추상) ← CharacterData / MonsterData / PetData
2. 공통 필드: ID, 이름(LocalizationKey), 프로필 이미지, Prefab 참조(Addressables 또는 직접), 기본 스탯, 성장 커브, 스킬 리스트(SkillData[]), 속성/등급/태그, 표현 트랙 기본값(피격/사망/승리 등)
3. MonsterData 고유: AI 패턴 훅, 드랍 테이블(기존 DropTable SO 재사용), 스폰 풀 연동
4. PetData 고유: 장착 슬롯(주인 연결), 보조 효과(버프 트리거)
5. EntityComponent (MonoBehaviour): StatContainer(StatSystem 연결), BuffOwner, SkillExecutor, PresentationAnchor 포함. Compose over inherit
6. 스폰 매니저: Prefab을 ObjectPool로 관리, 엔티티 ID 발급, ServiceLocator 등록
7. 상태 머신: Idle/Move/Cast/Hit/Dead. 상태별 표현 훅 지점 명시
8. 이벤트: EntitySpawned/Despawned/Died/StatChanged/StateChanged
9. 기존 시스템 연결: StatSystem, BuffSystem, SkillSystem, ObjectPool, SaveSystem(플레이어 보유 캐릭터 영속화)
10. 디자이너가 SO + Prefab 조합만으로 새 캐릭터/몬스터 추가 가능

결과물:
- 파일별 구현 계획
- 의존 다이어그램
- 샘플 3종(플레이어 캐릭터 1, 몬스터 1, 펫 1) SO 구성 예시
- 프로젝트 복사 재사용 체크리스트
```

---

## 4차 — 아이템 / 인벤토리 / 장비 연동

### 선행 조건
- 3차 병합 완료

### 범위
- 포함: ItemData 계층(Consumable/Material/Equipment/Currency), Inventory 코어, 스택/슬롯 정책, 획득/사용/폐기 파이프라인, 기존 EnhanceSystem 장비 연동, 획득 소스(가챠/퀘스트/우편/드랍/상점) 통합
- 제외: 상점 UI(7차), 거래/마켓

### 프롬프트

```
/ultraplan PublicProject에 1~3차(Presentation + Skill + Entity)가 병합되어 있다. 이제 아이템 / 인벤토리 / 장비 연동을 설계하라.

요구사항:
1. ScriptableObject 계층: ItemData(추상) ← ConsumableItemData / MaterialItemData / EquipmentItemData / CurrencyItemData
2. 공통 필드: ID, 이름/설명(LocalizationKey), 아이콘, 등급, 스택 정책, 태그, 획득/사용 표현 트랙
3. ConsumableItemData: 사용 효과 리스트(ISkillEffect 재사용 — 스킬과 동일 컴포저블)
4. EquipmentItemData: 슬롯 타입, 기본 옵션, EnhanceConfig 참조, 기존 EquipmentInstanceData 연동
5. Inventory 코어: 슬롯 수 제한, 카테고리별 필터, 스택 병합, 정렬 전략
6. 파이프라인: AddItem(출처) → 스택 병합 → 보관 → 이벤트 발행 → 표현 트랙 재생
7. 획득 소스 통합: 가챠 보상, 퀘스트 보상, 우편 보상, 몬스터 드랍, 상점 구매 → 모두 IRewardHandler로 통일(기존 계약 재사용)
8. 장비 장착/해제는 EntityComponent와 연결, StatContainer에 Equipment 레이어 modifier 주입(기존 StatSystem 재사용)
9. SaveSystem 영속화
10. 이벤트: ItemAdded/Removed/Used/StackChanged/InventoryFull/Equipped/Unequipped

결과물:
- 파일별 구현 계획
- 기존 EnhanceSystem / Gacha / Quest / Mail 과의 통합 다이어그램
- IRewardHandler 표준 구현 예시
- 샘플 10종 아이템 SO 구성
```

---

## 5차 — 전투 루프 / AI

### 선행 조건
- 4차 병합 완료

### 범위
- 포함: 전투 컨트롤러(턴/실시간 선택 가능), 페이즈(준비/교전/정산), 몬스터 AI 기본(IAIStrategy), 데미지/힐 파이프라인 정리, 전투 HUD 연결
- 제외: 스테이지 선택 UI(7차), 보상 정산 UI(7차)

### 프롬프트

```
/ultraplan PublicProject에 1~4차 완료 후, 전투 루프와 AI를 설계하라.

요구사항:
1. IBattleController (턴제 / 실시간 전환 가능한 추상). 전략 패턴으로 구현체 교체 가능
2. 전투 페이즈: Preparation → Engagement(루프) → Resolution(보상 정산 → IRewardHandler 호출)
3. IAIStrategy (Aggressive/Defensive/Support/Custom). OCP로 확장
4. 데미지 파이프라인: AttackerStat → DefenderStat → Mitigation → Crit → Final (기존 StatSystem 활용). ElementalAffinity 훅
5. 기존 IBattleHUD/IBattleWidget과 EventBus로 느슨 결합
6. 전투 시작/종료 플로우: EntityComponent 스폰 → 스킬 큐 → 매 tick 또는 턴마다 행동 결정 → 스킬 실행 → 페이즈 전이
7. 패배/승리 조건 플러그인화(ICompletionRule — TimeLimit, AllEnemyDead, TargetProtect 등)
8. 전투 기록(BattleLog)으로 리플레이/분석 확장 여지
9. 이벤트: BattleStarted/Ended/PhaseChanged/WaveStarted/DamageDealt/EntityAction

결과물:
- 파일별 구현 계획
- 턴제와 실시간 두 샘플 전투 시나리오 플로우
- 기존 HUD 시스템 연결 지점
- 확장 체크리스트
```

---

## 6차 — 게임 UI 콘텐츠 (로비 / 인벤토리 UI / 캐릭터 / 상점 등)

### 선행 조건
- 4차 이상 병합(전투 UI는 5차 선행)

### 범위
- 포함: 기존 UI 프레임워크(Screen/Popup/Overlay) 위에 실제 화면들을 표준 구성으로 제공. 로비, 캐릭터 상세, 인벤토리, 상점, 퀘스트 보드, 우편함(기존), 업적(기존), 설정, 가챠(기존) 화면
- 제외: 전투 내부 HUD(이미 BaseBattleHUD 존재)

### 프롬프트

```
/ultraplan PublicProject에 1~4차(또는 5차) 병합. 기존 UI 프레임워크(3레이어 Screen/Popup/Overlay, 전환/애니메이션 전략, TMP 헬퍼, Attribute)를 그대로 활용해 실제 게임 화면 콘텐츠 세트를 설계하라.

요구사항:
1. 표준 Screen: Lobby, CharacterSelect, CharacterDetail, Inventory, Shop, QuestBoard, Settings, GachaLobby
2. 표준 Popup: ItemDetail, SkillDetail, ConfirmPurchase, RewardReceive, EquipCompare
3. 표준 Overlay: CurrencyBar, EventBanner, LoadingIndicator(기존 재활용)
4. 각 Screen은 BaseScreen 계승, Popup은 BasePopup 계승, Overlay는 BaseOverlay 계승
5. 데이터 바인딩: 기존 CurrencyDisplay / StatDisplay / BuffIconBinder / LocalizedTMPText 재사용
6. SO 기반 구성: LayoutConfig SO로 슬롯 수/그리드/탭 구성을 데이터화
7. 입력: New Input System 이용. 전환은 기존 IScreenTransition 교체 가능
8. 세이프에어리어/레이아웃은 기존 SafeAreaFitter/AutoLayoutRefresher 활용
9. 로컬라이제이션: 모든 텍스트는 LocalizationKey Attribute로 표식
10. 각 화면 완성 조건: 데이터 로드 플로우 / 이벤트 구독 / 표현 트랙 트리거 / 닫힘 시 리소스 해제

결과물:
- 화면별 파일 구성(Screen/Popup MonoBehaviour + Prefab 구조 권장안)
- 표준 조립 가이드(새 화면 추가 5스텝)
- 각 화면의 의존 시스템 매트릭스
- 프로젝트 복사 재사용 체크리스트
```

---

## 7차 — 저작 도구 (에디터)

### 선행 조건
- 2~4차 병합(스킬/엔티티/아이템 데이터가 있어야 의미 있음)

### 범위
- 포함: 기획자용 에디터 윈도우 — 스킬 빌더, 캐릭터/몬스터 브라우저, 퀘스트 빌더, 표현 트랙 프리뷰어, SO 일괄 검증기
- 제외: 런타임 치트 도구(별도 Phase)

### 프롬프트

```
/ultraplan PublicProject 콘텐츠 레이어(1~6차 일부) 기반. 기획자·디자이너가 SO와 Prefab을 빠르게 저작·검증할 에디터 도구를 설계하라.

요구사항:
1. SkillBuilderWindow: SkillData SO를 단계별 위저드로 생성(타깃팅 → 효과 조립 → 표현 트랙 연결 → 프리뷰)
2. EntityBrowserWindow: CharacterData/MonsterData/PetData를 필터·정렬·다중 편집
3. QuestBuilderWindow: QuestData SO 생성, ConditionGroup을 Flow 노드로 편집
4. PresentationPreviewer: PresentationTrack SO를 에디터 플레이 모드 없이 시각 프리뷰(가능 범위 내) + 실제 씬 테스트 러너
5. SO Validator: 모든 콘텐츠 SO에 대해 누락 필드/중복 ID/로컬라이제이션 키 누락 일괄 검사
6. 기존 Attribute(ReadOnly/LocalizationKey/SceneName/EnumFlags/ShowIf) 적극 활용
7. 에디터 전용 코드는 Editor 폴더에 격리, 런타임 빌드 영향 없음
8. CLAUDE.md 네이밍 준수, Editor asmdef 분리

결과물:
- 윈도우별 파일/구조 계획
- 우선 순위(저작 빈도 높은 것부터)
- 확장 훅(프로젝트별 커스텀 검증 규칙 추가 절차)
```

---

## 진행 순서 권장

```
1차 (Presentation) ─┬─► 2차 (Skill) ─┬─► 3차 (Entity) ─┬─► 4차 (Item/Inventory) ─┬─► 5차 (Battle/AI)
                   │                │                  │                         │
                   └── 이후 모든 시스템이 이 위에 얹힘                               ├─► 6차 (UI Contents)
                                                                                 │
                                                                                 └─► 7차 (Authoring Tools)
```

- **1차는 반드시 최우선**. 이후 모든 차수가 표현 훅에 의존.
- 5차(전투)·6차(UI)는 병렬 가능하지만, 전투 HUD 확정이 필요하면 5차를 먼저.
- 7차(저작 도구)는 2~4차 데이터가 쌓이면 착수. 늦어도 4차 직후 권장.

---

## ultraplan 호출 체크리스트

매 차수 시작 전:

- [ ] 이전 차수 병합 완료 + `origin/main` push
- [ ] `SYSTEMS.md` 업데이트 반영
- [ ] CLAUDE.md 규칙 변경 없음 확인
- [ ] `/tasks`로 잔여 세션 정리
- [ ] 프롬프트를 그대로 `/ultraplan <프롬프트>` 실행
- [ ] 브라우저에서 섹션별 인라인 코멘트로 조정
- [ ] 승인 후 터미널 텔레포트 또는 웹 실행 선택
