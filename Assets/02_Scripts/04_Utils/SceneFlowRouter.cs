using UnityEngine.SceneManagement;

namespace PublicFramework
{
    /// <summary>
    /// 씬 전환 단일 진입점. ISceneLoader 가 등록돼 있으면 그것으로 전환하고(게임이 로딩스크린/페이드/
    /// 애널리틱스 등 전환 정책을 한 곳에서 장악), 없으면 raw SceneManager.LoadScene 으로 폴백한다.
    ///
    /// ISceneLoader 등록은 파생 게임의 선택사항 — 미등록이어도 단순 전환이 그대로 동작한다.
    /// 모든 씬 로드 호출은 이 라우터를 거쳐, 전환 훅이 필요한 게임은 ISceneLoader 하나만 등록하면 된다.
    /// </summary>
    public static class SceneFlowRouter
    {
        /// <summary>지정 씬으로 전환. ISceneLoader 우선, 없으면 raw 폴백.</summary>
        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (ServiceLocator.Has<ISceneLoader>())
            {
                ServiceLocator.Get<ISceneLoader>().LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>현재 활성 씬을 다시 로드(재도전 등).</summary>
        public static void Reload()
        {
            Load(SceneManager.GetActiveScene().name);
        }
    }
}
