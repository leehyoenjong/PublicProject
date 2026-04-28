using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 챕터 정의 ScriptableObject. ChapterData 시트 1행 = 1 SO.
    /// </summary>
    [CreateAssetMenu(fileName = "NewChapterData", menuName = "PublicFramework/Stage/ChapterData")]
    public class ChapterData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _chapterId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField] private Sprite _keyVisual;

        [Header("분류")]
        [SerializeField] private ChapterType _chapterType;
        [SerializeField, SheetAlias("order")] private int _sortOrder;

        [Header("선행")]
        [SerializeField, SheetAlias("unlockChapterMID")] private string _unlockChapterId;

        [Header("완주 보상")]
        [SerializeField] private QuestReward[] _chapterCompleteRewards;

        public string ChapterId => _chapterId;
        public int DisplayName => _displayName;
        public int Description => _description;
        public Sprite KeyVisual => _keyVisual;
        public ChapterType ChapterType => _chapterType;
        public int SortOrder => _sortOrder;
        public string UnlockChapterId => _unlockChapterId;
        public IReadOnlyList<QuestReward> ChapterCompleteRewards => _chapterCompleteRewards;
    }
}
