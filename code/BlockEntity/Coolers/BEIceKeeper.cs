namespace FoodShelves;

public class BEIceKeeper : BEBaseFSAnimatable {
    protected new BlockIceKeeper block = null!;
    public override string AttributeCheck => FSCoolingOnly;
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    [TreeSerializable(false)] public bool DoorOpen { get; set; }

    protected enum SlotType {
        Door = 0,
        Keeper = 1
    }

    public BEIceKeeper() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockIceKeeper)!;
        base.Initialize(api);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        if ((SlotType)blockSel.SelectionBoxIndex == SlotType.Door) {
            ToggleDoor(!DoorOpen, byPlayer);
            MarkDirty(true);
            return true;
        }

        return false;
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
            PerishMultiplier = 1;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.WallCabinetOpen, byPlayer, byPlayer, true, 16);
            }
        }
        else {
            AnimUtil.TryStopAnimation("dooropen");
            PerishMultiplier = 0;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.WallCabinetClose, byPlayer, byPlayer, true, 16, 0.3f);
            }
        }

        DoorOpen = open;
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilJarLarge, 9f);
        if (contentMesh != null) mesher.AddMeshData(contentMesh);

        return true;
    }

    protected override float[][]? genTransformationMatrices() => null; // Unneeded
}
