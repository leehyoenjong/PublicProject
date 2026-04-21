using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 시트 임포터가 생성한 개별 SO 인스턴스들을 모아 런타임에 한 번에 참조하기 위한 추상 컬렉션.
    /// 구체 클래스는 T를 고정하여 상속한다. 예: QuestDataCollection : DataCollection{QuestData}.
    /// </summary>
    public abstract class DataCollection<T> : ScriptableObject where T : ScriptableObject
    {
        [SerializeField] protected T[] _items;

        public IReadOnlyList<T> Items => _items;

#if UNITY_EDITOR
        /// <summary>에디터 전용: 임포터가 정렬된 항목 배열로 컬렉션 내용을 일괄 교체.</summary>
        public void SetItems(T[] items)
        {
            _items = items ?? Array.Empty<T>();
        }
#endif
    }
}
