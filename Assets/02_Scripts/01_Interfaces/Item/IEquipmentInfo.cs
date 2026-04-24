namespace PublicFramework
{
    public interface IEquipmentInfo : IItemSubtypeInfo
    {
        int ItemMID { get; }
        string SlotId { get; }
        int SetId { get; }
    }
}

