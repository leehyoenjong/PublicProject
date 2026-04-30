using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PublicFramework
{
    /// <summary>
    /// Screen 스택 기반 UI 매니저 구현.
    /// Push/Pop 패턴으로 Screen 전환을 관리한다.
    /// </summary>
    public class UIManager : IUIManager
    {
        private const int MAX_HISTORY_DEPTH = 10;

        private readonly Stack<BaseScreen> _screenStack = new();
        private readonly Dictionary<string, BaseScreen> _screenPrefabs = new();
        private readonly Dictionary<string, BaseScreen> _screenInstances = new();
        private readonly Transform _screenRoot;
        private readonly MonoBehaviour _coroutineRunner;
        private bool _isTransitioning;

        public int ScreenCount => _screenStack.Count;

        public UIManager(Transform screenRoot, MonoBehaviour coroutineRunner)
        {
            _screenRoot = screenRoot ?? throw new ArgumentNullException(nameof(screenRoot));
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            Debug.Log("[UI] 초기화 완료.");
        }

        public void RegisterScreen(string screenId, BaseScreen prefab)
        {
            if (_screenPrefabs.ContainsKey(screenId))
            {
                Debug.LogWarning($"[UI] 스크린 '{screenId}' 이미 등록됨. 덮어씀.");
            }
            _screenPrefabs[screenId] = prefab;
        }

        public void Push(string screenId, IScreenTransition transition = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[UI] 전환 진행 중. Push 무시됨.");
                return;
            }

            var newScreen = GetOrCreateScreen(screenId);
            if (newScreen == null) return;

            BaseScreen currentScreen = _screenStack.Count > 0 ? _screenStack.Peek() : null;

            _screenStack.Push(newScreen);
            EnforceHistoryDepth();

            if (transition != null && currentScreen != null)
            {
                _coroutineRunner.StartCoroutine(
                    ExecuteTransition(currentScreen, newScreen, transition, isReplace: false));
            }
            else
            {
                currentScreen?.Hide();
                newScreen.Show();
                newScreen.OnScreenEnter();
            }
        }

        public void Pop(IScreenTransition transition = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[UI] 전환 진행 중. Pop 무시됨.");
                return;
            }

            if (_screenStack.Count <= 1)
            {
                Debug.LogWarning("[UI] 마지막 스크린은 Pop할 수 없음.");
                return;
            }

            var currentScreen = _screenStack.Pop();
            var previousScreen = _screenStack.Peek();

            if (transition != null)
            {
                _coroutineRunner.StartCoroutine(
                    ExecutePopTransition(currentScreen, previousScreen, transition));
            }
            else
            {
                currentScreen.OnScreenExit();
                currentScreen.Hide();
                previousScreen.Show();
            }
        }

        public void Replace(string screenId, IScreenTransition transition = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[UI] 전환 진행 중. Replace 무시됨.");
                return;
            }

            var newScreen = GetOrCreateScreen(screenId);
            if (newScreen == null) return;

            BaseScreen currentScreen = _screenStack.Count > 0 ? _screenStack.Pop() : null;

            _screenStack.Push(newScreen);

            if (transition != null && currentScreen != null)
            {
                _coroutineRunner.StartCoroutine(
                    ExecuteTransition(currentScreen, newScreen, transition, isReplace: true));
            }
            else
            {
                if (currentScreen != null)
                {
                    currentScreen.OnScreenExit();
                    currentScreen.Hide();
                }
                newScreen.Show();
                newScreen.OnScreenEnter();
            }
        }

        public void ClearAndPush(string screenId, IScreenTransition transition = null)
        {
            while (_screenStack.Count > 0)
            {
                var screen = _screenStack.Pop();
                screen.OnScreenExit();
                screen.Hide();
            }

            Push(screenId, transition);
        }

        public BaseScreen GetCurrentScreen()
        {
            return _screenStack.Count > 0 ? _screenStack.Peek() : null;
        }

        private BaseScreen GetOrCreateScreen(string screenId)
        {
            if (_screenInstances.TryGetValue(screenId, out var existing))
            {
                return existing;
            }

            if (!_screenPrefabs.TryGetValue(screenId, out var prefab))
            {
                Debug.LogError($"[UI] 스크린 '{screenId}'이(가) 등록되지 않음.");
                return null;
            }

            var instance = Object.Instantiate(prefab, _screenRoot);
            instance.gameObject.SetActive(false);
            _screenInstances[screenId] = instance;
            return instance;
        }

        private void EnforceHistoryDepth()
        {
            if (_screenStack.Count <= MAX_HISTORY_DEPTH) return;

            var tempList = new List<BaseScreen>(_screenStack);
            _screenStack.Clear();

            for (int i = MAX_HISTORY_DEPTH - 1; i >= 0; i--)
                _screenStack.Push(tempList[i]);

            for (int i = MAX_HISTORY_DEPTH; i < tempList.Count; i++)
            {
                tempList[i].OnScreenExit();
                tempList[i].Hide();
            }

            Debug.LogWarning($"[UI] 히스토리 깊이 초과. {MAX_HISTORY_DEPTH}개로 잘림.");
        }

        private IEnumerator ExecuteTransition(
            BaseScreen from, BaseScreen to, IScreenTransition transition, bool isReplace)
        {
            _isTransitioning = true;

            to.gameObject.SetActive(true);
            to.CanvasGroup.alpha = 0f;

            yield return transition.Execute(from.CanvasGroup, to.CanvasGroup);

            if (isReplace)
                from.OnScreenExit();

            from.Hide();
            to.Show();
            to.OnScreenEnter();

            _isTransitioning = false;
        }

        private IEnumerator ExecutePopTransition(
            BaseScreen current, BaseScreen previous, IScreenTransition transition)
        {
            _isTransitioning = true;

            previous.gameObject.SetActive(true);
            previous.CanvasGroup.alpha = 0f;

            yield return transition.Execute(current.CanvasGroup, previous.CanvasGroup);

            current.OnScreenExit();
            current.Hide();
            previous.Show();

            _isTransitioning = false;
        }
    }
}
