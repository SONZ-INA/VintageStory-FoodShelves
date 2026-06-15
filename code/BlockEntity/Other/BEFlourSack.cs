namespace FoodShelves;

public class BEFlourSack : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only flour can be placed in this sack.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    protected override float PerishMultiplier => 0.6f;

    public BEFlourSack() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 8, true)); }

    protected override void InitMesh() {
        if (capi == null) return;

        if (!inv[0].Empty) {
            string? flourtype = inv[0].Itemstack?.Collectible.Variant["type"];
            VariantAttributes.SetString("seed", flourtype);
            base.InitMesh();
        }
        else {
            blockMesh = GenBlockVariantMesh(capi, this.GetVariantStack(), ["sackicon"]);
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block, and multiplayer icon sync

        mesher.AddMeshData(blockMesh);

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilFlourSack, 13f);
        if (contentMesh != null) mesher?.AddMeshData(contentMesh);
        
        return true;
    }

    protected override float[][]? genTransformationMatrices() => null; // Unneeded
}
