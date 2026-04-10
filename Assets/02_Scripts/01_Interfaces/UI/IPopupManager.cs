using System;

namespace PublicFramework
{
    /// <summary>
    /// 팝업 매니저 인터페이스.
    /// Priority Queue 기반으로 팝업 표시/관리.
    /// </summary>
    public interface IPopupManager : IService
    {
        void Show(string popupId, object data = null, int priority = 0);
        void Hide();
        void HideAll();
        BasePopup GetCurrentPopup();
        int PopupCount { get; }
        void RegisterPopup(string popupId, BasePopup prefab);
        event Action<BasePopup> OnPopupOpened;
        event Action<BasePopup> OnPopupClosed;
    }
}
