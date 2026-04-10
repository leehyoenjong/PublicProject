using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 화면 전환 애니메이션 인터페이스.
    /// </summary>
    public interface IScreenTransition
    {
        IEnumerator Execute(CanvasGroup from, CanvasGroup to);
    }
}
