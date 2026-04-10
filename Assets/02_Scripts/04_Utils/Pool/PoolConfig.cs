using System;
using UnityEngine;

namespace PublicFramework
{
    [Serializable]
    public class PoolConfig
    {
        [SerializeField] private string _id;
        [SerializeField] private int _initialSize = 5;
        [SerializeField] private int _maxSize = 20;
        [SerializeField] private bool _autoExpand = true;

        public string Id { get => _id; set => _id = value; }
        public int InitialSize => _initialSize;
        public int MaxSize => _maxSize;
        public bool AutoExpand => _autoExpand;

        public PoolConfig() { }

        public PoolConfig(string id, int initialSize, int maxSize, bool autoExpand)
        {
            _id = id;
            _initialSize = initialSize;
            _maxSize = maxSize;
            _autoExpand = autoExpand;
        }
    }
}
