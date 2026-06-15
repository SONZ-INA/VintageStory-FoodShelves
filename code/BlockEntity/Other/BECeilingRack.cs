namespace FoodShelves;

public class BECeilingRack : BEBaseFSContainer {
    protected MeshData? ropeMesh;

    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

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

        if (slot.Empty) {
            if (!inv[0].Empty) return TryTake(byPlayer, blockSel);
            if (!inv[1].Empty) return TryTakeFromSlot(byPlayer, inv[1]);

            return false;
        }

        if (inv[1].Empty) {
            if (slot.CanStoreInSlot(AttributeCheck)) {
                if (slot.TryPutInto(Api.World, inv[1]) > 0) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }

            return false;
        }

        return base.OnInteract(byPlayer, blockSel, "fsLiquidyStuff");
    }

    protected override float[][]? genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            // Hide original contents, can't bother to mesh it out
            if (td.index == 0) {
                td.hidden = true;
            }
        });
    }

    private MeshData? GenerateRopeMesh(ITesselatorAPI tesselator) {
        Shape? rackRope = Api.Assets.TryGet(ShapeReferences.utilCeilingRack)?.ToObject<Shape>();
        if (rackRope == null) return null;

        tesselator.TesselateShape(block, rackRope, out MeshData? ropeMesh);
        return ropeMesh;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        if (base.BaseRenderContents(mesher, tesselator))
            return true;

        if (!inv[1].Empty) {
            ropeMesh ??= GenerateRopeMesh(tesselator);
            mesher.AddMeshData(ropeMesh);
        }

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilJarLarge, 8.5f);
        if (contentMesh != null) mesher.AddMeshData(contentMesh);

        mesher.AddMeshData(blockMesh);

        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Dry, TransitionDisplayMode.Percentage));
    }
}
