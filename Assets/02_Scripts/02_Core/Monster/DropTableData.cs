using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 드롭 테이블 SO. MonsterDrop 시트에서 parentId(=MID) 로 묶인 행이 _entries 로 주입된다.
    /// 여러 몬스터가 같은 테이블을 공유 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDropTable", menuName = "PublicFramework/Monster/DropTable")]
    public class DropTableData : ScriptableObject, IDropTable
    {
        [SerializeField, SheetAlias("MID")] private string _mid;
        [SerializeField] private DropEntry[] _entries;

        public string MID => _mid;

        public IReadOnlyList<IDropEntry> Entries
        {
            get
            {
                if (_entries == null) return System.Array.Empty<IDropEntry>();
                var list = new IDropEntry[_entries.Length];
                for (int i = 0; i < _entries.Length; i++) list[i] = _entries[i];
                return list;
            }
        }
    }
}
