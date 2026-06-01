using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStageSelection 기본 구현. 단일 stageId 상태만 보유하는 순수 C#.
    /// </summary>
    public class StageSelection : IStageSelection
    {
        public string SelectedStageId { get; private set; }

        public void Select(string stageId)
        {
            SelectedStageId = stageId;
            Debug.Log($"[스테이지선택] 선택됨: {stageId}");
        }

        public void Clear()
        {
            SelectedStageId = null;
        }
    }
}
