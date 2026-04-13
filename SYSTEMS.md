# PublicProject 시스템 가이드

> 전체 197개 C# 파일, 22개 시스템. 네임스페이스: `PublicFramework`
> 수정이 필요할 때 이 문서에서 해당 시스템과 파일을 찾아 참조한다.

---

## 아키텍처 개요

```
01_Interfaces/  → 인터페이스, 추상 클래스 (계약)
02_Core/        → 순수 C# 구현 (MonoBehaviour 의존 없음)
03_Mono/        → MonoBehaviour 구현체 (Unity 컴포넌트)
04_Utils/       → 유틸리티, 확장 메서드
09_ScriptableObjects/ → SO 설정 파일
```

모든 시스템은 **ServiceLocator**에 등록하여 사용한다:
```csharp
ServiceLocator.Register<IXxxSystem>(new XxxSystem(...));
var system = ServiceLocator.Get<IXxxSystem>();
```

시스템 간 통신은 **EventBus**를 사용한다:
```csharp
eventBus.Subscribe<SomeEvent>(handler);
eventBus.Publish(new SomeEvent { ... });
```

---

## Phase 1 — 기반 시스템

### 1. ServiceLocator
> 서비스 등록/조회. 모든 시스템의 진입점.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Core/IService.cs` | 마커 인터페이스 |
| `02_Core/Core/ServiceLocator.cs` | static 클래스. Register/Get/Has/Unregister |

**수정 포인트**: 새 시스템 추가 시 `IService`를 상속한 인터페이스 생성 후 ServiceLocator에 등록.

---

### 2. EventBus
> 시스템 간 느슨한 결합 Pub/Sub.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Events/IEventBus.cs` | Subscribe/Unsubscribe/Publish/Clear |
| `02_Core/Events/EventBus.cs` | Delegate.Combine 기반 구현 |
| `03_Mono/Events/EventListener.cs` | MonoBehaviour 자동 구독/해제 |

**수정 포인트**: 새 이벤트 추가 시 `struct` 정의 후 `Publish/Subscribe`.

---

### 3. 세이브/로드
> 슬롯(5개) 기반 키-값 저장. AES 암호화 선택적.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Save/ISaveSystem.cs` | SetData/GetData/Save/Load/Delete |
| `01_Interfaces/Save/IDataSerializer.cs` | 직렬화 전략 |
| `01_Interfaces/Save/IDataEncryptor.cs` | 암호화 전략 |
| `01_Interfaces/Save/ISaveStorage.cs` | 저장소 전략 (로컬/클라우드) |
| `02_Core/Save/SaveSystem.cs` | 핵심 구현 |
| `02_Core/Save/SaveSlot.cs` | 슬롯 데이터 |
| `02_Core/Save/JsonDataSerializer.cs` | JSON 직렬화 |
| `02_Core/Save/BinaryDataSerializer.cs` | Binary 직렬화 |
| `02_Core/Save/AesDataEncryptor.cs` | AES-256 암호화 |
| `02_Core/Save/LocalSaveStorage.cs` | 로컬 파일 저장 |
| `03_Mono/Save/AutoSaveManager.cs` | 주기적 자동 저장 |
| `04_Utils/Save/SavePathHelper.cs` | 경로 헬퍼 |

**수정 포인트**: 클라우드 저장 → `ISaveStorage` 구현체 추가. 암호화 변경 → `IDataEncryptor` 교체.

---

### 4. 오브젝트 풀링
> Queue 기반 풀, 자동 확장, IPoolable 콜백.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Pool/IPoolable.cs` | OnSpawn/OnDespawn 콜백 |
| `01_Interfaces/Pool/IObjectPoolManager.cs` | 풀 관리 인터페이스 |
| `02_Core/Pool/ObjectPool.cs` | 단일 풀 |
| `02_Core/Pool/ObjectPoolManager.cs` | 복수 풀 관리 |
| `03_Mono/Pool/PoolableObject.cs` | MonoBehaviour 풀 대상 |
| `03_Mono/Pool/PoolInitializer.cs` | 풀 초기화 |
| `04_Utils/Pool/PoolConfig.cs` | 풀 설정 |

---

### 5. 씬 관리
> SceneParam 타입 안전 전달, Additive 로딩, ISceneFlow 교체.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Scene/ISceneFlow.cs` | 씬 흐름 전략 |
| `01_Interfaces/Scene/ISceneLoader.cs` | 씬 로드 인터페이스 |
| `01_Interfaces/Scene/ISceneParam.cs` | 씬 전달 파라미터 |
| `02_Core/Scene/SceneFlowBase.cs` | 씬 흐름 베이스 |
| `03_Mono/Scene/SceneLoader.cs` | 씬 로더 |
| `03_Mono/Scene/SceneTransitionRunner.cs` | 전환 연출 |
| `03_Mono/Scene/BootScene.cs` | 부트 씬 |

---

### 6. 사운드
> BGM 크로스페이드, SFX 풀링(10), 4채널 볼륨, 채널별 뮤트.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Sound/ISoundManager.cs` | 사운드 인터페이스 |
| `02_Core/Sound/SoundData.cs` | 사운드 데이터 |
| `03_Mono/Sound/SoundManager.cs` | 사운드 매니저 |
| `03_Mono/Sound/SoundPlayer.cs` | 개별 플레이어 |

---

### 7. UI 프레임워크
> 3레이어(Screen/Popup/Overlay), 스택 전환, Priority Queue 팝업.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/UI/IUIManager.cs` | 화면 관리 |
| `01_Interfaces/UI/IPopupManager.cs` | 팝업 관리 |
| `01_Interfaces/UI/IScreenTransition.cs` | 화면 전환 전략 |
| `01_Interfaces/UI/IPopupAnimation.cs` | 팝업 애니메이션 전략 |
| `01_Interfaces/UI/IBattleHUD.cs` | 전투 HUD |
| `01_Interfaces/UI/IBattleWidget.cs` | HUD 위젯 |
| `02_Core/UI/ScreenType.cs` | 화면 타입 enum |
| `02_Core/UI/PopupData.cs` | 팝업 데이터 |
| `02_Core/UI/PopupResult.cs` | 팝업 결과 |
| `02_Core/UI/DamageType.cs` | 데미지 타입 |
| `02_Core/UI/Transitions/Fade,Slide,None` | 전환 구현체 3종 |
| `02_Core/UI/Animations/Fade,ScaleBounce` | 애니메이션 2종 |
| `03_Mono/UI/UIManager.cs` | UI 매니저 |
| `03_Mono/UI/PopupManager.cs` | 팝업 매니저 |
| `03_Mono/UI/BaseScreen.cs` | 화면 베이스 |
| `03_Mono/UI/BasePopup.cs` | 팝업 베이스 |
| `03_Mono/UI/BaseOverlay.cs` | 오버레이 베이스 |
| `03_Mono/UI/BaseBattleHUD.cs` | 전투 HUD 베이스 |
| `03_Mono/UI/Popups/Alert,Confirm,Loading,Toast` | 기본 팝업 4종 |
| `09_ScriptableObjects/UI/UITransitionConfig.cs` | 전환 설정 SO |
| `09_ScriptableObjects/UI/BattleHUDConfig.cs` | HUD 설정 SO |

---

## Phase 2 — 전투 기반

### 8. StatSystem (스탯)
> 4레이어 계산: `Final = (Base + Growth + EquipFlat + BuffFlat) × (1 + EquipPercent + BuffPercent)`

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Stat/IStatSystem.cs` | CreateContainer/GetContainer/RemoveContainer |
| `01_Interfaces/Stat/IStatContainer.cs` | GetFinalValue/AddModifier/RemoveModifier |
| `01_Interfaces/Stat/IStatModifier.cs` | TargetStat/ModType/Value/Priority/Layer/Source |
| `02_Core/Stat/StatType.cs` | ATK,DEF,HP,MaxHP,SPD,CritRate,CritDamage 등 10종 |
| `02_Core/Stat/StatEnums.cs` | StatModType(Flat/Percent), StatLayer(Base/Growth/Equipment/Buff) |
| `02_Core/Stat/StatModifier.cs` | IStatModifier 불변 구현체 |
| `02_Core/Stat/StatContainer.cs` | 4레이어 계산, 자동 재계산 |
| `02_Core/Stat/StatSystem.cs` | 엔티티별 StatContainer 관리 |
| `02_Core/Stat/StatEvents.cs` | StatChangedEvent, ModifierAdded/Removed |

**수정 포인트**: 새 스탯 추가 → `StatType` enum에 추가. 계산 공식 변경 → `StatContainer.RecalculateStat()`.

---

### 9. 버프/디버프
> 중첩 4종, 지속시간 4종, 면역, IBuffEffect 확장.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Buff/IBuffSystem.cs` | AddBuff/RemoveBuff/Tick/ProcessTurn |
| `01_Interfaces/Buff/IBuffInstance.cs` | 런타임 버프 상태 |
| `01_Interfaces/Buff/IBuffEffect.cs` | 커스텀 확장점 (OnApply/OnTick/OnRemove/OnStack) |
| `01_Interfaces/Buff/IBuffUIData.cs` | UI 표시 데이터 |
| `02_Core/Buff/BuffEnums.cs` | ModifierType(7), BuffCategory(3), StackPolicy(4), DurationType(4), RefreshPolicy(4) |
| `02_Core/Buff/BuffData.cs` | **SO** — 버프 정의 데이터 |
| `02_Core/Buff/BuffInstance.cs` | 런타임 인스턴스 |
| `02_Core/Buff/BuffSystem.cs` | 핵심 구현. StatSystem 연동 |
| `02_Core/Buff/BuffEvents.cs` | 7개 이벤트 (Applied/Removed/Expired/StackChanged/Refreshed/Tick/Immune) |
| `02_Core/Buff/BuffResult.cs` | AddBuff 반환값 |
| `03_Mono/Buff/BuffIconBar.cs` | 버프 아이콘 바 |
| `03_Mono/Buff/BuffIconSlot.cs` | 개별 아이콘 슬롯 |
| `03_Mono/Buff/BuffTooltip.cs` | 툴팁 팝업 |

**수정 포인트**: 새 버프 추가 → BuffData SO 생성. 커스텀 동작 → `IBuffEffect` 구현. 중첩 정책 → `StackPolicy` enum.

---

### 10. 장비 강화
> 레벨/등급/초월/각성 4단계. IEnhanceStrategy 전략 패턴.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Enhance/IEnhanceSystem.cs` | Enhance/CanEnhance/GetCost/RegisterStrategy |
| `01_Interfaces/Enhance/IEnhanceStrategy.cs` | 강화 로직 전략 |
| `01_Interfaces/Enhance/IProbabilityModel.cs` | 확률 판정 전략 |
| `02_Core/Enhance/EnhanceEnums.cs` | EnhanceType(4), FailPolicy(4), MaterialType(6), Grade(5) |
| `02_Core/Enhance/EnhanceSystem.cs` | 핵심 구현. 타입별 전략 위임 |
| `02_Core/Enhance/DefaultLevel/Grade/Transcend/AwakeningStrategy.cs` | 기본 전략 4종 |
| `02_Core/Enhance/DefaultProbabilityModel.cs` | 기본 확률 모델 (천장 포함) |
| `02_Core/Enhance/EquipmentInstanceData.cs` | [Serializable] 장비 데이터 |
| `02_Core/Enhance/AwakeningSlotData.cs` | [Serializable] 각성 슬롯 |
| `02_Core/Enhance/EnhanceResult/Cost/Context.cs` | 결과/비용/컨텍스트 구조체 |
| `02_Core/Enhance/EnhanceEvents.cs` | 6개 이벤트 |
| `09_ScriptableObjects/Enhance/EnhanceConfig.cs` | **SO** — 강화 설정 (확률/비용/등급 테이블) |

**수정 포인트**: 강화 로직 변경 → `IEnhanceStrategy` 새 구현체. 확률 모델 변경 → `IProbabilityModel` 교체. 수치 변경 → `EnhanceConfig` SO 수정.

---

## Phase 3 — 수익화

### 11. 가챠
> 배너/드롭테이블 SO, IPullStrategy, Hard/Soft/PickupGuarantee 천장.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Gacha/IGachaSystem.cs` | Pull/GetBannerInfo/GetPityInfo/GetProbabilities |
| `01_Interfaces/Gacha/IPullStrategy.cs` | 확률 모델 전략 |
| `01_Interfaces/Gacha/IGachaPresentation.cs` | 연출 전략 |
| `01_Interfaces/Gacha/IDuplicateHandler.cs` | 중복 처리 전략 |
| `02_Core/Gacha/GachaSystem.cs` | 핵심 구현 |
| `02_Core/Gacha/WeightedPullStrategy.cs` | 기본 가중치 뽑기 |
| `02_Core/Gacha/SoftPityPullStrategy.cs` | 소프트 천장 + PickupGuarantee |
| `02_Core/Gacha/GachaBannerData.cs` | **SO** — 배너 설정 |
| `02_Core/Gacha/DropTable.cs` | **SO** — 드롭 테이블 |
| `02_Core/Gacha/PityCounter.cs` | [Serializable] 천장 카운터 |
| `02_Core/Gacha/GachaEnums.cs` | GachaType(6), ItemGrade(5), PityType(4) 등 |
| `02_Core/Gacha/GachaEvents.cs` | 7개 이벤트 |
| `02_Core/Gacha/GachaResult/Reward.cs` | 결과/보상 구조체 |
| `03_Mono/Gacha/GachaBannerUI.cs` | 배너 선택 화면 |
| `03_Mono/Gacha/GachaResultUI.cs` | 결과 연출 |
| `03_Mono/Gacha/GachaProbabilityUI.cs` | 확률 표시 |

**수정 포인트**: 확률 모델 변경 → `IPullStrategy` 새 구현체. 배너 추가 → `GachaBannerData` SO 생성. 천장 수치 → SO 설정.

---

### 12. IAP (인앱결제)
> 5단계 구매 파이프라인. IStoreAdapter/IReceiptValidator 추상화.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/IAP/IIAPSystem.cs` | Purchase/RestorePurchases/ProcessPendingPurchases |
| `01_Interfaces/IAP/IStoreAdapter.cs` | 플랫폼 스토어 추상화 |
| `01_Interfaces/IAP/IReceiptValidator.cs` | 영수증 검증 추상화 |
| `02_Core/IAP/IAPSystem.cs` | 핵심 구현. 5단계 파이프라인 |
| `02_Core/IAP/DummyStoreAdapter.cs` | 개발용 더미 스토어 |
| `02_Core/IAP/AlwaysValidReceiptValidator.cs` | 개발용 더미 검증 |
| `02_Core/IAP/IAPProductData.cs` | [Serializable] 상품 데이터 |
| `02_Core/IAP/IAPRewardEntry.cs` | [Serializable] 보상 항목 |
| `02_Core/IAP/PurchaseReceipt.cs` | [Serializable] 영수증 |
| `02_Core/IAP/ReceiptValidationResult.cs` | 검증 결과 |
| `02_Core/IAP/SubscriptionData.cs` | [Serializable] 구독 데이터 (ImmediateRewards 포함) |
| `02_Core/IAP/IAPEnums.cs` | ProductType(4), PurchaseFailReason(8) 등 |
| `02_Core/IAP/IAPEvents.cs` | 6개 이벤트 |
| `09_ScriptableObjects/IAP/IAPConfig.cs` | **SO** — 상품 카탈로그 + 광고 슬롯 |

**수정 포인트**: 플랫폼 연동 → `IStoreAdapter` 구현 (GooglePlay, Apple). 서버 검증 → `IReceiptValidator` 구현. 상품 추가 → `IAPConfig` SO.

---

### 13. 광고
> Rewarded/Interstitial/Banner 3종. 일일 제한/쿨타임/VIP 면제.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Ad/IAdSystem.cs` | ShowAd/CanShowAd/GetRemainingWatches |
| `01_Interfaces/Ad/IAdAdapter.cs` | 광고 네트워크 추상화 |
| `02_Core/Ad/AdSystem.cs` | 핵심 구현. VIP=리워드만 허용 |
| `02_Core/Ad/DummyAdAdapter.cs` | 개발용 더미 |
| `02_Core/Ad/AdSlotData.cs` | [Serializable] 슬롯 데이터 |
| `02_Core/Ad/AdEnums.cs` | AdType(3), AdFailReason(7) |
| `02_Core/Ad/AdEvents.cs` | 4개 이벤트 |

**수정 포인트**: 광고 SDK 연동 → `IAdAdapter` 구현 (AdMob 등). VIP 정책 → `AdSystem.CanShowAd()`.

---

## Phase 4 — 콘텐츠 시스템

### 14. 조건 시스템 (공유)
> 퀘스트/업적이 공유하는 조건 추적 인프라.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Condition/ICondition.cs` | AddProgress/IsCompleted/Reset |
| `01_Interfaces/Condition/IConditionProgress.cs` | UI용 진행 데이터 |
| `02_Core/Condition/ConditionEnums.cs` | ConditionType(11종), ConditionGroupType(All/Any/Sequence) |
| `02_Core/Condition/ConditionData.cs` | [Serializable] 조건 데이터 |
| `02_Core/Condition/Condition.cs` | ICondition 구현체 |
| `02_Core/Condition/ConditionGroup.cs` | 복합 조건 (All/Any/Sequence) |
| `02_Core/Condition/ConditionEvents.cs` | ConditionProgressEvent (범용) |

**수정 포인트**: 새 조건 유형 → `ConditionType` enum 추가 + Tracker에서 이벤트 매핑.

---

### 15. 퀘스트
> 7종 타입, QuestTracker 자동 추적, Daily/Weekly 리셋.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Quest/IQuestSystem.cs` | AcceptQuest/ClaimReward/GetQuests/ResetDaily |
| `01_Interfaces/Quest/IQuestInstance.cs` | 퀘스트 인스턴스 |
| `01_Interfaces/Quest/IRewardHandler.cs` | 보상 처리 위임 (퀘스트/업적 공유) |
| `02_Core/Quest/QuestSystem.cs` | 핵심 구현 |
| `02_Core/Quest/QuestTracker.cs` | EventBus 자동 추적 |
| `02_Core/Quest/QuestData.cs` | **SO** — 퀘스트 정의 |
| `02_Core/Quest/QuestInstance.cs` | 런타임 인스턴스 |
| `02_Core/Quest/QuestEnums.cs` | QuestType(7), QuestState(5) |
| `02_Core/Quest/QuestEvents.cs` | 8개 이벤트 (Progress 포함) |
| `02_Core/Quest/QuestReward.cs` | [Serializable] 보상 |
| `03_Mono/Quest/QuestListUI.cs` | 퀘스트 목록 |
| `03_Mono/Quest/QuestSlotUI.cs` | 개별 슬롯 |
| `03_Mono/Quest/QuestDetailUI.cs` | 상세 화면 |

**수정 포인트**: 보상 지급 연동 → `IRewardHandler` 구현. 퀘스트 추가 → `QuestData` SO 생성.

---

### 16. 업적
> 다단계 Tier, 포인트 마일스톤, 칭호, 상시 추적.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Achievement/IAchievementSystem.cs` | ClaimReward/GetTotalPoints/ClaimMilestone |
| `01_Interfaces/Achievement/IAchievementInstance.cs` | 업적 인스턴스 |
| `02_Core/Achievement/AchievementSystem.cs` | 핵심 구현 |
| `02_Core/Achievement/AchievementTracker.cs` | EventBus 상시 추적 |
| `02_Core/Achievement/AchievementData.cs` | **SO** — 업적 정의 |
| `02_Core/Achievement/AchievementInstance.cs` | 런타임 인스턴스 |
| `02_Core/Achievement/AchievementTierData.cs` | [Serializable] 단계별 보상 |
| `02_Core/Achievement/AchievementEnums.cs` | Category(9), State(4) |
| `02_Core/Achievement/AchievementEvents.cs` | 6개 이벤트 |
| `02_Core/Achievement/PointMilestone.cs` | [Serializable] 포인트 마일스톤 |
| `03_Mono/Achievement/AchievementListUI.cs` | 업적 목록 |
| `03_Mono/Achievement/AchievementSlotUI.cs` | 개별 슬롯 |
| `03_Mono/Achievement/AchievementDetailUI.cs` | 상세 화면 |

---

### 17. 우편함
> 5종 타입, IMailProvider 서버 추상화, 만료/중복방지/최대보관.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Mail/IMailSystem.cs` | SendMail/ClaimMail/ClaimAll/ProcessExpired |
| `01_Interfaces/Mail/IMailProvider.cs` | 서버 연동 추상화 |
| `02_Core/Mail/MailSystem.cs` | 핵심 구현. 중복 수령 방지(State 선변경) |
| `02_Core/Mail/LocalMailProvider.cs` | 로컬 전용 기본 구현 |
| `02_Core/Mail/MailData.cs` | [Serializable] 우편 데이터 |
| `02_Core/Mail/MailRewardEntry.cs` | [Serializable] 보상 |
| `02_Core/Mail/MailboxSaveData.cs` | [Serializable] 세이브 |
| `02_Core/Mail/MailEnums.cs` | MailType(5), MailState(4) |
| `02_Core/Mail/MailEvents.cs` | 7개 이벤트 |
| `09_ScriptableObjects/Mail/MailConfig.cs` | **SO** — 최대 보관(100), 만료(30일), 자동삭제(3일) |
| `03_Mono/Mail/MailboxUI.cs` | 우편함 화면 |
| `03_Mono/Mail/MailDetailPopup.cs` | 상세 팝업 |
| `03_Mono/Mail/MailBadge.cs` | 미수령 배지 |

**수정 포인트**: 서버 연동 → `IMailProvider` 구현. 보관 수/만료일 → `MailConfig` SO.

---

## Phase 5 — 글로벌/UX

### 18. 다국어 (Localization)
> 11개 언어, Fallback(현재→En→Ko→Key), 실시간 갱신.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Localization/ILocalizationSystem.cs` | SetLanguage/GetText/HasKey |
| `01_Interfaces/Localization/ILocalizationLoader.cs` | 데이터 로드 전략 |
| `02_Core/Localization/LocalizationSystem.cs` | 핵심 구현. Fallback + string.Format |
| `02_Core/Localization/LocalizationTable.cs` | **SO** — 텍스트 테이블 |
| `02_Core/Localization/FontMapping.cs` | **SO** — 언어별 폰트 매핑 |
| `02_Core/Localization/CsvLocalizationLoader.cs` | CSV 파싱 로더 |
| `02_Core/Localization/JsonLocalizationLoader.cs` | JSON 파싱 로더 |
| `02_Core/Localization/LocalizationEnums.cs` | LanguageCode(11종) |
| `02_Core/Localization/LocalizationEvents.cs` | 3개 이벤트 |
| `03_Mono/Localization/LocalizedText.cs` | 텍스트 자동 교체 컴포넌트 |
| `03_Mono/Localization/LocalizedFont.cs` | 폰트 자동 교체 |
| `03_Mono/Localization/LocalizedImage.cs` | 이미지 자동 교체 |

**수정 포인트**: 번역 추가 → CSV/JSON 파일 또는 `LocalizationTable` SO. 새 언어 → `LanguageCode` enum 추가.

---

### 19. 튜토리얼
> 스텝 순차 실행, 조건부 트리거, 하이라이트/대화/화살표.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Tutorial/ITutorialSystem.cs` | StartTutorial/NextStep/SkipTutorial/CheckTriggers |
| `01_Interfaces/Tutorial/ITutorialPresentation.cs` | 연출 교체 전략 |
| `02_Core/Tutorial/TutorialSystem.cs` | 핵심 구현. 트리거 매칭 + Priority 정렬 |
| `02_Core/Tutorial/TutorialData.cs` | **SO** — 튜토리얼 정의 |
| `02_Core/Tutorial/TutorialStepData.cs` | [Serializable] 스텝 데이터 |
| `02_Core/Tutorial/TutorialEnums.cs` | StepType(5), TriggerType(8), WaitType(5) 등 |
| `02_Core/Tutorial/TutorialEvents.cs` | 5개 이벤트 |
| `03_Mono/Tutorial/TutorialOverlay.cs` | 오버레이 + 마스크 |
| `03_Mono/Tutorial/TutorialDialog.cs` | 대화 표시 |
| `03_Mono/Tutorial/TutorialArrow.cs` | 화살표 가이드 |

**수정 포인트**: 튜토리얼 추가 → `TutorialData` SO 생성. 연출 변경 → `ITutorialPresentation` 구현.

---

## Phase 6 — 인프라

### 20. 서버 통신 (Network)
> INetworkClient, 지수 백오프 재시도, 401 토큰 갱신, 요청 큐.

> ⚠️ **변경 예정 (2026-04-13 기록)**: 범용 HTTP/REST 기반 `NetworkClient`를 **뒤끝서버(TheBackend)** 기반으로 재설계 예정.
> - **뒤끝 베이스**: https://docs.backnd.com/sdk-docs/backend/base/start-up/ (로그인/유저 데이터/리더보드 등)
> - **뒤끝 데이터베이스**: https://docs.backnd.com/sdk-docs/database/intro/ (차트/게시판/우편 등 — 베이스와 별개 제품)
> - 진행 방식: 기획자 → 개발자 → QA 4인 개발 사이클(신규 세션에서 착수).
> - 범용 `NetworkClient` 유지 여부는 기획 단계에서 결정 (공존 vs 전환).

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Network/INetworkClient.cs` | SendRequest/CancelAll/IsConnected |
| `01_Interfaces/Network/IAuthTokenProvider.cs` | 토큰 관리 추상화 |
| `01_Interfaces/Network/INetworkSerializer.cs` | 직렬화 추상화 |
| `02_Core/Network/NetworkClient.cs` | 핵심 구현. UnityWebRequest + Coroutine |
| `02_Core/Network/RequestQueue.cs` | 우선순위 큐, 동시 제한 |
| `02_Core/Network/NetworkRequest.cs` | 요청 데이터 |
| `02_Core/Network/NetworkResponse.cs` | 응답 데이터 |
| `02_Core/Network/RequestOptions.cs` | 재시도/타임아웃 설정 |
| `02_Core/Network/DummyAuthTokenProvider.cs` | 개발용 더미 |
| `02_Core/Network/JsonNetworkSerializer.cs` | JsonUtility 기반 |
| `02_Core/Network/NetworkEnums.cs` | HttpMethod(5), NetworkError(9), Priority(4) |
| `02_Core/Network/NetworkEvents.cs` | 6개 이벤트 |
| `09_ScriptableObjects/Network/NetworkConfig.cs` | **SO** — BaseUrl, 타임아웃, 동시 요청 수 |

**수정 포인트**: 서버 인증 → `IAuthTokenProvider` 구현. API URL → `NetworkConfig` SO. 동시 요청 수 → SO 설정.

---

### 21. 푸시 알림 (Notification)
> 로컬/리모트, 채널 관리, 딥링크, 스케줄 저장.

| 파일 | 역할 |
|------|------|
| `01_Interfaces/Notification/INotificationSystem.cs` | Schedule/Cancel/채널 ON-OFF/권한 |
| `01_Interfaces/Notification/IRemotePushProvider.cs` | FCM/APNs 추상화 |
| `01_Interfaces/Notification/IDeepLinkHandler.cs` | 딥링크 라우팅 |
| `02_Core/Notification/NotificationSystem.cs` | 핵심 구현 |
| `02_Core/Notification/DummyRemotePushProvider.cs` | 개발용 더미 |
| `02_Core/Notification/DefaultDeepLinkHandler.cs` | 기본 딥링크 핸들러 |
| `02_Core/Notification/NotificationData.cs` | [Serializable] 알림 데이터 |
| `02_Core/Notification/NotificationChannel.cs` | [Serializable] 채널 데이터 |
| `02_Core/Notification/NotificationEnums.cs` | Type, Importance, Permission |
| `02_Core/Notification/NotificationEvents.cs` | 6개 이벤트 |
| `09_ScriptableObjects/Notification/NotificationConfig.cs` | **SO** — 채널 설정 |

**수정 포인트**: FCM 연동 → `IRemotePushProvider` 구현. 채널 추가 → `NotificationConfig` SO.

---

## 시스템 간 의존 관계

```
ServiceLocator ← 모든 시스템 등록
EventBus ← 모든 시스템이 이벤트 발행/구독
SaveSystem ← 대부분 시스템이 데이터 저장

StatSystem ← BuffSystem (Buff 레이어)
           ← EnhanceSystem (Equipment 레이어)

ICondition ← QuestSystem (조건 추적)
           ← AchievementSystem (조건 추적)

IRewardHandler ← QuestSystem (보상 지급)
               ← AchievementSystem (보상 지급)

IPullStrategy ← GachaSystem (확률 모델)
IEnhanceStrategy ← EnhanceSystem (강화 로직)
IStoreAdapter ← IAPSystem (플랫폼 스토어)
IAdAdapter ← AdSystem (광고 네트워크)
IMailProvider ← MailSystem (서버 우편)
IRemotePushProvider ← NotificationSystem (리모트 푸시)
```

---

## ScriptableObject 설정 파일 목록

| SO | 경로 | 주요 설정 |
|----|------|-----------|
| UITransitionConfig | `09_SO/UI/` | 화면 전환 설정 |
| BattleHUDConfig | `09_SO/UI/` | HUD 설정 |
| EnhanceConfig | `09_SO/Enhance/` | 강화 확률/비용/등급 테이블 |
| IAPConfig | `09_SO/IAP/` | 상품 카탈로그 + 광고 슬롯 |
| MailConfig | `09_SO/Mail/` | 최대 보관/만료/자동삭제 |
| NetworkConfig | `09_SO/Network/` | BaseUrl/타임아웃/동시 요청 |
| NotificationConfig | `09_SO/Notification/` | 알림 채널 설정 |

> BuffData, QuestData, AchievementData, TutorialData, GachaBannerData, DropTable, LocalizationTable, FontMapping은 `02_Core/` 하위의 SO이며 에디터에서 다수 생성하여 사용.

---

## 커밋 이력

| 해시 | Phase | 파일 수 |
|------|-------|---------|
| `52ddc81` | Initial commit | - |
| `5874d5a` | Phase 1: 기반 시스템 | 60 |
| `b6e3e66` | Phase 2: 스탯/버프/강화 | 39 |
| `cacbb0b` | Phase 3: 가챠/IAP/광고 | 38 |
| `f669604` | Phase 4: 퀘스트/업적/우편함 | 46 |
| `95219cb` | Phase 5+6: 다국어/튜토리얼/네트워크/푸시 | 46 |

---

## 프로젝트별 구현 필요 사항

새 프로젝트에서 이 프레임워크를 사용할 때 구현해야 하는 인터페이스:

| 인터페이스 | 용도 | 프레임워크 기본 |
|-----------|------|----------------|
| `IRewardHandler` | 보상 지급 (재화/아이템) | 없음 (필수 구현) |
| `IStoreAdapter` | 플랫폼 스토어 연동 | DummyStoreAdapter |
| `IReceiptValidator` | 영수증 서버 검증 | AlwaysValidReceiptValidator |
| `IAdAdapter` | 광고 SDK 연동 | DummyAdAdapter |
| `IMailProvider` | 서버 우편 연동 | LocalMailProvider |
| `IAuthTokenProvider` | 서버 인증 토큰 | DummyAuthTokenProvider |
| `IRemotePushProvider` | FCM/APNs 연동 | DummyRemotePushProvider |
| `IGachaPresentation` | 가챠 연출 | 없음 (선택 구현) |
| `ITutorialPresentation` | 튜토리얼 연출 | 기본 UI 3종 제공 |
| `IDuplicateHandler` | 가챠 중복 처리 | 없음 (선택 구현) |
| `ISceneFlow` | 씬 전환 흐름 | SceneFlowBase |
