using System.Linq;

namespace FoodShelves;

public class BESeedBins : BEBaseFSContainer {
    protected new BaseFSContainer block;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    protected override string CantPlaceMessage => "foodshelves:Only seeds can be placed in these jars.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegmentGrouped;

    protected override float PerishMultiplier => 0.75f; // AoG Compatibility

    public override int ShelfCount => 4;
    public override int ItemsPerSegment => 6;

    public BESeedBins() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BaseFSContainer;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        var stacks = GetContentStacks();
        List<string> dontRender = [];

        for (int i = 0; i < 4; i++) {
            // Content
            var stacksForMesh = stacks.Skip(i * ItemsPerSegment).Take(ItemsPerSegment).ToArray();
            contentMeshes[i] = GenLiquidyMesh(capi, stacksForMesh, ShapeReferences.utilSeedBins, 6f, false)?.Translate(0, .04f, 0).BlockYRotation(block);
            
            // Icon
            if (capi == null) continue;

            if (stacksForMesh?[0]?.Collectible != null) {
                string seedtype = stacksForMesh[0].Collectible.Variant["type"];
                VariantAttributes.SetString($"seed{i}", seedtype);
            }
            else {
                dontRender.Add($"Lid{i}Icon"); // If no contents are present for this segment, filter it out.
            }
        }

        blockMesh = GenBlockVariantMesh(capi, this.GetVariantStack(), [.. dontRender]);
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        for (int i = 0; i < 4; i++) {
            if (contentMeshes[i] == null) continue;
            MeshData contentMesh = contentMeshes[i].Clone();

            switch (block.GetRotationAngle()) {
                case 0: contentMesh.Translate(i % 2 * 0.4695f, i / 2 * 0.4695f, 0); break;
                case 90: contentMesh.Translate(0, i / 2 * 0.4695f, -i % 2 * 0.4695f); break;
                case 180: contentMesh.Translate(-i % 2 * 0.4695f, i / 2 * 0.4695f, 0); break;
                case 270: contentMesh.Translate(0, i / 2 * 0.4695f, i % 2 * 0.4695f); break;
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
