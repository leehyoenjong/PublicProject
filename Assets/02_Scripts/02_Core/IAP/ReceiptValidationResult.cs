namespace PublicFramework
{
    /// <summary>
    /// 영수증 검증 결과 구조체
    /// </summary>
    public struct ReceiptValidationResult
    {
        public bool IsValid;
        public ReceiptValidationError Error;
        public string TransactionId;
    }
}
