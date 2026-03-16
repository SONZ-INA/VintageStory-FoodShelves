namespace FoodShelves;

public class BEBreadShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only bread, muffins, dumplings, pacoca, halva or marzipam can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByShelf;
    protected override float PerishMultiplier => 0.5f;

    public BEBreadShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 2;
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
            td.x = td.segment * 0.3f - 0.3f;
            td.y = td.shelf * 0.25f + 0.0625f;
            td.z = td.item * 0.375f - 0.225f;
        });
    }
}
