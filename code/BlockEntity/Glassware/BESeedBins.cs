namespace FoodShelves;

public class BESeedBins : BEBaseFSContainer {
    protected new BaseFSContainer block = null!;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    protected override string CantPlaceMessage => "foodshelves:Only seeds can be placed in these jars.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegmentGrouped;

    protected override float PerishMultiplier => 0.65f; // Compatibility with mods that add perish rate to seeds

    public override int ShelfCount => 4;

    public BESeedBins() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 6, true)); }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BaseFSContainer)!;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        // Check which icons to avoid rendering
        List<string> dontRender = [];

        for (int i = 0; i < 4; i++) {
            ItemStack? stack = inv[i].Itemstack;

            if (stack?.Collectible != null) {
                string seedtype = stack.Collectible.Variant["type"];
                VariantAttributes.SetString($"seed{i}", seedtype);
            }
            else {
                dontRender.Add($"Lid{i}Icon"); // If no contents are present for this segment, filter it out.
            }
        }

        blockMesh = GenBlockVariantMesh(capi, this.GetVariantStack(), [.. dontRender]);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        MarkDirty(true);
        return base.OnInteract(byPlayer, blockSel, overrideAttrCheck);
    }

    protected override float[][]? genTransformationMatrices() => null; // Unneeded

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh();

        base.OnTesselation(mesher, tesselator);

        BlockDirection rot = (BlockDirection)block.GetRotationAngle();

        for (int i = 0; i < 4; i++) {
            MeshData? contentMesh = GenLiquidyMesh(capi, inv[i], ShapeReferences.utilSeedBins, 6f, false)?.BlockYRotation(block);
            if (contentMesh == null) continue;

            const float offset = 0.4695f;
            int col = i % 2;
            int row = i / 2;

            switch (rot) {
                case BlockDirection.North: contentMesh.Translate(col * offset, row * offset, 0); break;
                case BlockDirection.West: contentMesh.Translate(0, row * offset, -col * offset); break;
                case BlockDirection.South: contentMesh.Translate(-col * offset, row * offset, 0); break;
                case BlockDirection.East: contentMesh.Translate(0, row * offset, col * offset); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex * ItemsPerSegment;
        sb.AppendLine(GetNutrientRequirement(Api.World, inv[index].Itemstack));
    }
}
