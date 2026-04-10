using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 팝업 출현/퇴장 애니메이션 인터페이스.
    /// </summary>
    public interface IPopupAnimation
    {
        IEnumerator PlayShow(RectTransform popup, CanvasGroup canvasGroup);
        IEnumerator PlayHide(RectTransform popup, CanvasGroup canvasGroup);
    }
}
