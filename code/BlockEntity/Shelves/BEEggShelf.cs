namespace FoodShelves;

public class BEEggShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only eggs can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BEEggShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 5;
        ItemsPerSegment = 4;
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
        return TransformationGenerator.Generate(this, td => {
            td.x = td.segment * 0.171f - 0.34f;
            td.y = td.shelf * 0.25f + 0.0625f;
            td.z = td.item * 0.1875f - 0.3125f;
        });
    }
}
