namespace FoodShelves;

public class BEIceKeeper : BEBaseFSAnimatable {
    protected new BlockIceKeeper block = null!;
    public override string AttributeCheck => FSCoolingOnly;
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    [TreeSerializable(false)] public bool DoorOpen { get; set; }

    protected enum SlotType {
        Keeper = 0,
        Door = 1
    }

    public BEIceKeeper() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 16, true)); }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockIceKeeper)!;
        base.Initialize(api);

        MeltingMultiplier = DoorOpen ? 1f : 0f;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Melt) {
            return MeltingMultiplier;
        }

        return base.Inventory_OnAcquireTransitionSpeed(transType, stack, baseMul);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        if ((SlotType)blockSel.SelectionBoxIndex == SlotType.Door) {
            ToggleDoor(!DoorOpen, byPlayer);
            MarkDirty(true);
            return true;
        }

        return base.OnInteract(byPlayer, blockSel, overrideAttrCheck);
    }

    #region Animations

    protected override void HandleAnimations() {
        if (AnimUtil == null)
            return;

        if (DoorOpen) ToggleDoor(true);
        else ToggleDoor(false);
    }

    protected void ToggleDoor(bool open, IPlayer? byPlayer = null) {
        if (open) {
            AnimUtil.TryStartAnimation("dooropen", 3f);
            MeltingMultiplier = 1;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.IceKeeperOpen, byPlayer, byPlayer, true, 16, 0.5f);
            }
        }
        else {
            AnimUtil.TryStopAnimation("dooropen");
            MeltingMultiplier = 0;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.IceKeeperClose, byPlayer, byPlayer, true, 16, 0.7f);
            }
        }

        DoorOpen = open;
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilIceKeeper, 15.75f);
        if (contentMesh != null) mesher.AddMeshData(contentMesh);

        return true;
    }

    protected override float[][]? genTransformationMatrices() => null; // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Melt, TransitionDisplayMode.TimeLeft));
    }
}
