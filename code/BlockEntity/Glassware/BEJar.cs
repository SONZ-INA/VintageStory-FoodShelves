namespace FoodShelves;

public class BEJar : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only Jars can be placed on this shelf.";

    public override string AttributeCheck => "fsLiquidyStuff";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    protected override float PerishMultiplier => 0.74f;
    protected override float DryingMultiplier => 4.5f; // Vanilla transition calculations are so fucked

    public BEJar() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, "fsLiquidyStuff", 2, true)); }

    public override void Initialize(ICoreAPI api) {
        inv.PerishableFactorByFoodCategory = new Dictionary<EnumFoodCategory, float>() {
            [EnumFoodCategory.Grain] = 0.5f
        };

        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        if (capi == null) return;

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilJar, 7.3f);
        if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);
    }

    protected override float[][]? genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Dry, TransitionDisplayMode.Percentage));
    }
}
