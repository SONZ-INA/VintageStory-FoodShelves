namespace FoodShelves;

public class ItemSlotFSUniversal : ItemSlot {
    private readonly string attributeCheck;
    private readonly int stackCountLimit;
    private readonly bool bulk;

    public ItemSlotFSUniversal(InventoryBase inventory, string attributeCheck, int stackCountLimit = 1, bool bulk = false) : base(inventory) {
        this.inventory = inventory;
        this.attributeCheck = attributeCheck;
        this.stackCountLimit = stackCountLimit;
        this.bulk = bulk;
    }

    public override int GetRemainingSlotSpace(ItemStack forItemstack) {
        int capacity = bulk
            ? forItemstack.Collectible.MaxStackSize * stackCountLimit
            : stackCountLimit;

        return capacity - StackSize;
    }
    
    public override bool CanTakeFrom(ItemSlot slot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
        return slot.CanStoreInSlot(attributeCheck) && base.CanTakeFrom(slot, priority);
    }

    public override bool CanHold(ItemSlot slot) {
        return slot.CanStoreInSlot(attributeCheck) && base.CanHold(slot);
    }

    public override int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1) {
        return this.TryPutIntoBulk(world, sinkSlot, quantity);
    }
}
