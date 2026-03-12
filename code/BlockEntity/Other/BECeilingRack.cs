namespace FoodShelves;

public class BECeilingRack : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;

    protected override float PerishMultiplier => 0.74f;
    protected override float DryingMultiplier => 4.5f; // Vanilla transition calculations are so fucked

    public override int AdditionalSlots => 1;

    public BECeilingRack() {
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id == 0) return new ItemSlotFSUniversal(inv, "fsLiquidyStuff", 12, true);
            else return new ItemSlotFSUniversal(inv, AttributeCheck);
        });
    }

    public override void Initialize(ICoreAPI api) {
        inv.PerishableFactorByFoodCategory = new Dictionary<EnumFoodCategory, float>() {
            [EnumFoodCategory.Grain] = 0.5f
        };

        base.Initialize(api);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) { // Take jar
            if (!inv[0].Empty) return TryTake(byPlayer, blockSel);
            if (!inv[1].Empty) return TryTakeFromSlot(byPlayer, inv[1]);

            return false;
        }

        if (inv[1].Empty) {
            if (slot.CanStoreInSlot(AttributeCheck)) { // Put jar in rack
                if (slot.TryPutInto(Api.World, inv[1]) > 0) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }

            return false;
        }

        // Large Jar interactions
        return base.OnInteract(byPlayer, blockSel, "fsLiquidyStuff");
    }

    protected override void InitMesh() {
        base.InitMesh();

        if (capi == null) return;

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilLargeJar, 8.5f);
        if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            if (!inv[1].Empty) {
                Shape? rackRope = Api.Assets.TryGet(ShapeReferences.utilCeilingRack)?.ToObject<Shape>();
                if (rackRope != null) {
                    tesselator.TesselateShape(block, rackRope, out MeshData ropeMesh);
                    blockMesh?.AddMeshData(ropeMesh);
                }
            }

            mesher.AddMeshData(blockMesh);
        }

        return true;
    }

    protected override float[][]? genTransformationMatrices() {
        return [new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values, new Matrixf().Values]; // Can't bother to mesh out the original contents
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Dry));
    }
}
