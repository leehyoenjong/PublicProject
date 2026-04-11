using System;

namespace PublicFramework
{
    /// <summary>
    /// 천장 카운터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class PityCounter
    {
        [UnityEngine.SerializeField] private string _bannerId;
        [UnityEngine.SerializeField] private int _pullCount;
        [UnityEngine.SerializeField] private bool _isGuaranteed;
        [UnityEngine.SerializeField] private long _lastPullTimeTicks;

        public string BannerId { get => _bannerId; set => _bannerId = value; }
        public int PullCount { get => _pullCount; set => _pullCount = value; }
        public bool IsGuaranteed { get => _isGuaranteed; set => _isGuaranteed = value; }

        public DateTime LastPullTime
        {
            get => new DateTime(_lastPullTimeTicks);
            set => _lastPullTimeTicks = value.Ticks;
        }

        public PityCounter(string bannerId)
        {
            _bannerId = bannerId;
            _pullCount = 0;
            _isGuaranteed = false;
            _lastPullTimeTicks = DateTime.MinValue.Ticks;
        }
    }
}
