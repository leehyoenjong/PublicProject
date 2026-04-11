using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 우선순위 기반 요청 큐. 동시 요청 제한.
    /// </summary>
    public class RequestQueue
    {
        private readonly int _maxConcurrent;
        private readonly List<QueueEntry> _pending = new List<QueueEntry>();
        private int _activeCount;

        public int PendingCount => _pending.Count;
        public int ActiveCount => _activeCount;

        public RequestQueue(int maxConcurrent)
        {
            _maxConcurrent = maxConcurrent;
        }

        public void Enqueue(NetworkRequest request, Action<NetworkRequest> executeAction)
        {
            _pending.Add(new QueueEntry
            {
                Request = request,
                ExecuteAction = executeAction
            });

            _pending.Sort((a, b) => b.Request.Priority.CompareTo(a.Request.Priority));

            TryProcessNext();
        }

        public void OnRequestComplete()
        {
            _activeCount = Mathf.Max(0, _activeCount - 1);
            TryProcessNext();
        }

        public bool Cancel(string requestId)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Request.RequestId == requestId)
                {
                    _pending.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void CancelAll()
        {
            _pending.Clear();
        }

        private void TryProcessNext()
        {
            while (_activeCount < _maxConcurrent && _pending.Count > 0)
            {
                QueueEntry entry = _pending[0];
                _pending.RemoveAt(0);
                _activeCount++;

                entry.ExecuteAction?.Invoke(entry.Request);
            }
        }

        private struct QueueEntry
        {
            public NetworkRequest Request;
            public Action<NetworkRequest> ExecuteAction;
        }
    }
}
