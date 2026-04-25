namespace PublicFramework
{
    /// <summary>덱 슬롯 멤버 변경.</summary>
    public struct DeckMemberChangedEvent
    {
        public string DeckId;
        public int Slot;
        public string OldInstanceId;
        public string NewInstanceId;
    }

    /// <summary>덱 리더 변경.</summary>
    public struct DeckLeaderChangedEvent
    {
        public string DeckId;
        public string OldLeaderId;
        public string NewLeaderId;
    }

    /// <summary>덱 이름 변경.</summary>
    public struct DeckRenamedEvent
    {
        public string DeckId;
        public string OldName;
        public string NewName;
    }

    /// <summary>덱 전체 초기화.</summary>
    public struct DeckClearedEvent
    {
        public string DeckId;
    }
}
