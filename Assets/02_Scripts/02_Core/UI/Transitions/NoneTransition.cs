using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 전환 효과 없이 즉시 교체.
    /// </summary>
    public class NoneTransition : IScreenTransition
    {
        public IEnumerator Execute(CanvasGroup from, CanvasGroup to)
        {
            from.alpha = 0f;
            to.alpha = 1f;
            yield break;
        }
    }
}
