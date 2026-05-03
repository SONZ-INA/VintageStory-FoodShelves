namespace FoodShelves;

public class BESushiShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only sushi can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BESushiShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 2;
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
        return TransformationGenerator.GenerateLayout(this, (t) => {
            t.scaleX = t.scaleY = 0.9f;

            t.offsetX = -0.3875f;
            t.offsetY = 0.05f;
            t.offsetZ = -0.255f;

            t.offsetRotY = 180;

            t.x = t.segment * 0.4525f + (t.item % 3) * 0.1325f;
            t.y = t.shelf * 0.25f;
            t.z = (t.item / 3) * 0.375f;
        });
    }
}
