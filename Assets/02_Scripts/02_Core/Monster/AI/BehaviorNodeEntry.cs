using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BT 노드 1개 직렬화. 트리는 인덱스 배열로 평탄화 (childIndices 가 자식 인덱스 가리킴).
    /// Action/Condition 일 때 actionKey + param1~3 사용. Composite/Decorator 는 childIndices 만 사용.
    /// Cooldown 노드는 param1 = cooldownSeconds 로 해석. Repeat 노드는 param1 = maxCount (0=무한).
    /// </summary>
    [Serializable]
    public class BehaviorNodeEntry
    {
        [SerializeField] private BehaviorNodeType _nodeType;
        [SerializeField] private int[] _childIndices;
        [SerializeField] private string _actionKey;
        [SerializeField] private string _param1;
        [SerializeField] private string _param2;
        [SerializeField] private string _param3;

        public BehaviorNodeType NodeType => _nodeType;
        public int[] ChildIndices => _childIndices;
        public string ActionKey => _actionKey;
        public string Param1 => _param1;
        public string Param2 => _param2;
        public string Param3 => _param3;

        public BehaviorNodeEntry() { }

        public BehaviorNodeEntry(BehaviorNodeType nodeType, int[] childIndices,
            string actionKey, string param1, string param2, string param3)
        {
            _nodeType = nodeType;
            _childIndices = childIndices;
            _actionKey = actionKey;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }
    }
}
