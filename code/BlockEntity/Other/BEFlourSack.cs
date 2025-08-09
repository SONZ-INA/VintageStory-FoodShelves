namespace FoodShelves;

public class BEFlourSack : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only flour can be placed in this sack.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;

    protected override float PerishMultiplier => 0.6f;

    public override int ItemsPerSegment => 4;

    public BEFlourSack() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64)); }

    protected override void InitMesh() {
        if (capi == null) return;

        if (!inv[0].Empty) {
            string flourtype = inv[0].Itemstack.Collectible.Variant["type"];
            VariantAttributes.SetString("seed", flourtype);
            base.InitMesh();
        }
        else {
            blockMesh = GenBlockVariantMesh(capi, this.GetVariantStack(), ["sackicon"]);
        }

        MeshData contentMesh = GenLiquidyMesh(capi, GetContentStacks(), ShapeReferences.utilFlourSack, 13f);
        if (contentMesh != null) blockMesh.AddMeshData(contentMesh);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        mesher.AddMeshData(blockMesh);
        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded
}
