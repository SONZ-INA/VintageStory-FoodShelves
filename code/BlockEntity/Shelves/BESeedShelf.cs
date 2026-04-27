namespace FoodShelves;

public class BESeedShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only seeds can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BESeedShelf() {
        ShelfCount = 3;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 4;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 1, true));
    }

    public override void Initialize(ICoreAPI api) {
        switch (Block.Variant["type"]) {
            case "short":
                ItemsPerSegment /= 2;
                this.RebuildInventory(api, 64);
                break;

            case "veryshort":
                ItemsPerSegment /= 4;
                this.RebuildInventory(api, 64);
                break;
        }

        base.Initialize(api);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.x = td.segment * 0.26f - 0.26f;
            td.y = td.shelf * 0.3125f + 0.0625f;
            td.z = td.item * 0.1875f - 0.33f;

            td.scaleX = td.scaleZ = 0.44f;
            td.scaleY = 0.35f;
        });
    }
}
