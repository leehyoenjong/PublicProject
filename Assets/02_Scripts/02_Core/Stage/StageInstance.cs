using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 인스턴스 런타임 상태.
    /// 클리어 횟수/별점/일일 입장 카운트/현재 wave 등을 추적.
    /// </summary>
    public class StageInstance
    {
        private readonly StageData _data;
        private StageState _state;
        private int _clearCount;
        private int _bestStars;
        private int _todayEnterCount;
        private string _lastEnterDateUtc;
        private int _currentWaveIndex;
        private float _elapsedSeconds;
        private readonly HashSet<int> _completedEventIndices = new HashSet<int>();

        public StageData Data => _data;
        public string StageId => _data.StageId;
        public StageType StageType => _data.StageType;
        public StageState State => _state;
        public int ClearCount => _clearCount;
        public bool IsFirstClear => _clearCount == 0;
        public int BestStars => _bestStars;
        public int TodayEnterCount => _todayEnterCount;
        public string LastEnterDateUtc => _lastEnterDateUtc;
        public int CurrentWaveIndex => _currentWaveIndex;
        public float ElapsedSeconds => _elapsedSeconds;
        public IReadOnlyCollection<int> CompletedEventIndices => _completedEventIndices;

        public StageInstance(StageData data)
        {
            _data = data;
            _state = StageState.Locked;
        }

        public void SetState(StageState state) => _state = state;
        public void SetCurrentWaveIndex(int index) => _currentWaveIndex = index;
        public void SetElapsed(float seconds) => _elapsedSeconds = seconds;

        public void RecordClear(int stars)
        {
            _clearCount++;
            if (stars > _bestStars) _bestStars = stars;
            _state = StageState.Cleared;
        }

        public void RecordEnter(string utcDate)
        {
            if (_lastEnterDateUtc != utcDate)
            {
                _todayEnterCount = 0;
                _lastEnterDateUtc = utcDate;
            }
            _todayEnterCount++;
        }

        public void MarkEventCompleted(int eventIndex)
        {
            _completedEventIndices.Add(eventIndex);
        }

        public bool IsEventCompleted(int eventIndex)
        {
            return _completedEventIndices.Contains(eventIndex);
        }

        public void ResetRuntime()
        {
            _currentWaveIndex = 0;
            _elapsedSeconds = 0f;
        }

        public void ResetEventCompletion()
        {
            _completedEventIndices.Clear();
        }
    }
}
