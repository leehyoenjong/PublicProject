using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 챕터/던전 클리어 시점에 프로젝트 코드에서 Fire(stageId) 호출.
    /// 프레임워크에 스테이지 이벤트 struct가 없어, 프로젝트가 자체 이벤트에서 이 컴포넌트를 호출하는 방식.
    /// </summary>
    public class TutorialStageClearTrigger : MonoBehaviour
    {
        public void Fire(string stageId)
        {
            ITutorialSystem tutorial = ServiceLocator.Get<ITutorialSystem>();
            if (tutorial == null) return;

            tutorial.CheckTriggers(TriggerType.StageClear, stageId);
        }
    }
}
