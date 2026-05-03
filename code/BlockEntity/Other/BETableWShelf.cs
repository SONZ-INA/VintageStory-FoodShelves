namespace FoodShelves;

public class BETableWShelf : BEBaseFSContainer {
    public override string AttributeCheck => "shelvable";
    public override string AttributeTransformCode => "onshelfTransform";

    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    
    protected override bool RipeningSpot => true;

    public override int ItemsPerSegment => 2;

    private enum TableWShelfPart {
        Table = 1,
        Shelf = 0
    }

    public BETableWShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf)
            return false;

        if (slot.Itemstack?.IsLargeItem() == true)
            return false;

        if (!inv[0].Empty && inv[0].Itemstack?.IsMediumItem() == true)
            return false;

        return base.TryPut(byPlayer, slot, blockSel);
    }

    protected override float[][] genTransformationMatrices() {  
        return TransformationGenerator.GenerateLayout(this, td => {
            td.z = td.item * 0.4f - 0.175f;
            td.y = 0.185f;

            if (!inv[td.index].Empty) {
                string itemPath = inv[td.index].Itemstack!.Collectible.Code.Path;

                if (itemPath.StartsWith("dirtyclaypot-") || itemPath.StartsWith("claypot-")) {
                    td.scaleX = td.scaleY = td.scaleZ = 0.85f;
                }

                if (itemPath.StartsWith("rollingpin-")) {
                    td.z = 0;
                }
            }
        });
    }
}
