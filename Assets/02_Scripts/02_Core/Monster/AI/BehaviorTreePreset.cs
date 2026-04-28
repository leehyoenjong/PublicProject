using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// AI 프리셋 SO. 노드 트리를 인덱스 평탄화 배열로 보유.
    /// 인스펙터에서 직접 편집하거나 Authoring 도구로 생성.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBehaviorTreePreset", menuName = "PublicFramework/Monster/Behavior Tree Preset")]
    public class BehaviorTreePreset : ScriptableObject
    {
        [Header("식별")]
        [SerializeField, SheetAlias("MID")] private string _presetId;

        [Header("트리")]
        [SerializeField] private int _rootIndex;
        [SerializeField] private BehaviorNodeEntry[] _nodes;

        public string PresetId => _presetId;
        public int RootIndex => _rootIndex;
        public IReadOnlyList<BehaviorNodeEntry> Nodes => _nodes;

        public BehaviorNodeEntry GetNode(int index)
        {
            if (_nodes == null || index < 0 || index >= _nodes.Length) return null;
            return _nodes[index];
        }
    }
}
