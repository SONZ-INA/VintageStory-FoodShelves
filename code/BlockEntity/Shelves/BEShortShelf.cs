namespace FoodShelves;

public class BEShortShelf : BEBaseFSContainer {
    public override string InventoryClassName => "shelf";
    public override string AttributeTransformCode => "onshelfTransform";

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 2;
    public override int SegmentsPerShelf => 2;

    public BEShortShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        bool ctrl = byPlayer.Entity.Controls.CtrlKey;

        // Crock sealing interactions
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if ((!slot.Empty || ctrl) && TryUse(byPlayer, blockSel))
            return true;

        return base.OnInteract(byPlayer, blockSel);
    }

    protected bool TryUse(IPlayer player, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;

        if (inv[index].Itemstack?.Collectible is IContainedInteractable ic) {
            MarkDirty();
            return ic.OnContainedInteractStart(this, inv[index], player, blockSel);
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.x = td.segment * 0.43f - 0.215f;
            td.y = td.shelf * 0.5f + 0.125f;
            td.z = -0.2f;
        });
    }
}
