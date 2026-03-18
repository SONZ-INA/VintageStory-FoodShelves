namespace FoodShelves;

public class BEFoodDisplayBlock : BEBaseFSContainer {
    public override string AttributeTransformCode => "onFoodUniversalTransform";

    public override string AttributeCheck => "fsFoodUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    protected override float PerishMultiplier => 0.75f;

    private enum SlotNumber {
        BottomSlot = 0,
        TopSlot = 1
    }

    public BEFoodDisplayBlock() {
        ShelfCount = 2;
        ItemsPerSegment = 4;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); 
    }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "top") {
            ShelfCount = 1;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
    }

    protected override int GetSegmentLimit(ItemStack? stack) {
        return SegmentLimits.Mixed(this, stack);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.y = td.shelf * 0.375f + 0.25f;
            td.z = 0.05f;

            ItemStack? stack = inv[td.index].Itemstack;
            if (stack == null) return;
            
            if (!stack.IsLargeItem() && !stack.IsMediumItem()) {
                td.z += td.item / 2 == 0 ? -0.05f : 0.05f;
            }
        }, true);
    }
}
