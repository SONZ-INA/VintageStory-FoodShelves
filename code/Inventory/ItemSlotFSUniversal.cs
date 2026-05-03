namespace FoodShelves;

public class ItemSlotFSUniversal : ItemSlot {
    public override int MaxSlotStackSize {
        get {
            if (!isBulk) return stackCountLimit;

            if (!Empty) {
                return (itemstack.Collectible?.MaxStackSize ?? 64) * stackCountLimit;
            }
            
            return 64 * stackCountLimit;
        }
        set => base.MaxSlotStackSize = value;
    }

    public readonly bool isBulk;

    private readonly string attributeCheck;
    private readonly int stackCountLimit;

    public ItemSlotFSUniversal(InventoryBase inventory, string attributeCheck, int stackCountLimit = 1, bool isBulk = false) : base(inventory) {
        this.inventory = inventory;
        this.attributeCheck = attributeCheck;
        this.stackCountLimit = stackCountLimit;
        this.isBulk = isBulk;
    }

    public override int GetRemainingSlotSpace(ItemStack forItemstack) {
        int capacity = isBulk
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
