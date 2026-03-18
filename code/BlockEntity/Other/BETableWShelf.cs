namespace FoodShelves;

public class BETableWShelf : BEBaseFSContainer {
    public override string AttributeCheck => "shelvable";
    public override string AttributeTransformCode => "onshelfTransform";

    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    
    protected override bool RipeningSpot => true;
    protected override bool IgnoreSegmentRestrictions => true;

    public override int ItemsPerSegment => 2;

    private enum TableWShelfPart {
        Table = 1,
        Shelf = 0
    }

    public BETableWShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf)
            return false;

        return base.TryPut(byPlayer, slot, blockSel);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.z = td.item * 0.4f - 0.175f;
            td.y = 0.185f;
        });
    }
}
