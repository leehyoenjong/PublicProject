using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 자동 저장 관리. 일정 간격으로 현재 슬롯을 디스크에 기록한다.
    /// </summary>
    public class AutoSaveManager : MonoBehaviour
    {
        [SerializeField] private float _autoSaveInterval = 300f;
        [SerializeField] private int _activeSlotIndex;

        private ISaveSystem _saveSystem;
        private float _timer;

        private void Start()
        {
            _saveSystem = ServiceLocator.Get<ISaveSystem>();
            Debug.Log($"[자동저장] 시작됨. 주기: {_autoSaveInterval}s, 슬롯: {_activeSlotIndex}");
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= _autoSaveInterval)
            {
                _timer = 0f;
                PerformAutoSave();
            }
        }

        private void PerformAutoSave()
        {
            if (_saveSystem == null)
            {
                Debug.LogError("[자동저장] SaveSystem 미등록.");
                return;
            }

            _saveSystem.WriteToDisk(_activeSlotIndex);
            Debug.Log($"[자동저장] 슬롯 {_activeSlotIndex} 저장됨.");
        }

        public void SetActiveSlot(int slotIndex)
        {
            _activeSlotIndex = slotIndex;
        }

        public void SetInterval(float seconds)
        {
            _autoSaveInterval = seconds;
        }

        public void SaveNow()
        {
            _timer = 0f;
            PerformAutoSave();
        }
    }
}
