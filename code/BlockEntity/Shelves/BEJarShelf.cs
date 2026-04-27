namespace FoodShelves;

public class BEJarShelf : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public override int ShelfCount => 2;
    public override int SegmentsPerShelf => 2;

    public BEJarShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "short") {
            ShelfCount = 1;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
    }

    protected override float[][]? genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, (t) => {
            t.offsetZ = Block.Variant["type"] == "short"
                ? -0.225f
                : 0.225f;

            t.offsetX = -0.2f;
            t.offsetY = 0.0625f;
            
            t.x = t.segment * 0.4f;
            t.z = t.shelf * -0.45f;
            t.y = t.shelf * 0.31f;
        });
    }
}
