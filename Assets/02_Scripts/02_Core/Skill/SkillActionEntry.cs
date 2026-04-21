using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 액션 블록 한 건. SkillAction 시트가 parentId/order 로 SkillData._actions 에 주입.
    /// param1~3 은 ActionType 별 규약에 따라 해석한다.
    /// </summary>
    [Serializable]
    public class SkillActionEntry
    {
        [SerializeField] private SkillActionType _actionType;
        [SerializeField] private float _delay;
        [SerializeField] private float _duration;
        [SerializeField] private string _param1;
        [SerializeField] private string _param2;
        [SerializeField] private string _param3;

        public SkillActionType ActionType => _actionType;
        public float Delay => _delay;
        public float Duration => _duration;
        public string Param1 => _param1;
        public string Param2 => _param2;
        public string Param3 => _param3;

        public SkillActionEntry() { }

        public SkillActionEntry(SkillActionType actionType, float delay, float duration, string param1, string param2, string param3)
        {
            _actionType = actionType;
            _delay = delay;
            _duration = duration;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }
    }
}
