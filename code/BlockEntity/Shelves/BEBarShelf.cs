namespace FoodShelves;

public class BEBarShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only food bars can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BEBarShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 6;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck));
    }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "short") {
            ItemsPerSegment /= 2;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.x = td.segment * 0.2825f - 0.2825f;
            td.y = td.shelf * 0.25f + 0.115f;
            td.z = td.item * 0.125f - 0.365f;
            
            td.rotY = 90;
            td.rotZ = 67.5f;

            td.scaleX = td.scaleY = td.scaleZ = 0.605f;
        });
    }
}
