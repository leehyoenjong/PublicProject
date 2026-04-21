using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 하이라이트/액션 대상 UI에 붙여 ID-RectTransform 매핑을 제공.
    /// OnEnable 시 static 레지스트리에 등록, OnDisable 시 해제 — 씬 전환/비활성 시 자동 정리.
    /// 동일 id 중복 등록 시 첫 등록자 유지 + 경고.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TutorialTarget : MonoBehaviour
    {
        [SerializeField] private string _id;

        private static readonly Dictionary<string, TutorialTarget> _registry = new Dictionary<string, TutorialTarget>();

        public string Id => _id;
        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_id)) return;

            if (_registry.TryGetValue(_id, out TutorialTarget existing) && existing != null && existing != this)
            {
                Debug.LogWarning($"[TutorialTarget] Duplicate ID '{_id}' — keeping first registration on '{existing.gameObject.name}'. Skipping '{gameObject.name}'.");
                return;
            }
            _registry[_id] = this;
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(_id)) return;
            if (_registry.TryGetValue(_id, out TutorialTarget existing) && existing == this)
            {
                _registry.Remove(_id);
            }
        }

        public static bool TryFind(string id, out RectTransform rectTransform)
        {
            rectTransform = null;
            if (string.IsNullOrEmpty(id)) return false;
            if (!_registry.TryGetValue(id, out TutorialTarget target) || target == null) return false;
            rectTransform = target.RectTransform;
            return rectTransform != null;
        }
    }
}
