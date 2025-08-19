namespace FoodShelves;

public class BECeilingJar : BEBaseFSContainer {
    public override string AttributeCheck => "fsLiquidyStuff";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;

    protected override float PerishMultiplier => 0.75f;
    protected override float DryingMultiplier => 1.25f;

    public override int ItemsPerSegment => 12;

    public BECeilingJar() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64)); }

    public override void Initialize(ICoreAPI api) {
        // Adjust when Storage Vessels are fixed
        inv.PerishableFactorByFoodCategory = new Dictionary<EnumFoodCategory, float>() { [EnumFoodCategory.Grain] = 0.5f };

        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        if (capi == null) return;

        MeshData contentMesh = GenLiquidyMesh(capi, GetContentStacks(), ShapeReferences.utilCeilingJar, 8.5f);
        if (contentMesh != null) blockMesh.AddMeshData(contentMesh);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        mesher.AddMeshData(blockMesh);
        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Dry));
    }
}
