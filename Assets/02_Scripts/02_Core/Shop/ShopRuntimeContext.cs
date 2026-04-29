using System;

namespace PublicFramework
{
    /// <summary>
    /// IShopContext 의 런타임 디폴트 구현. ShopHost 가 보유하며 PlayerLevel 과 quest resolver 를 외부에서 주입.
    /// IQuestSystem 직접 의존 회피 — Func&lt;int,bool&gt; 위임으로 호출자(외부 어댑터)가 통합점 결정.
    /// </summary>
    public class ShopRuntimeContext : IShopContext
    {
        private int _playerLevel;
        private Func<int, bool> _questResolver;

        public ShopRuntimeContext(int initialPlayerLevel = 1, Func<int, bool> questResolver = null)
        {
            _playerLevel = initialPlayerLevel;
            _questResolver = questResolver;
        }

        public int PlayerLevel => _playerLevel;

        public bool IsQuestCleared(int questMID)
        {
            if (_questResolver == null) return false;
            return _questResolver(questMID);
        }

        public void SetPlayerLevel(int level) => _playerLevel = level;
        public void SetQuestResolver(Func<int, bool> resolver) => _questResolver = resolver;
    }
}
