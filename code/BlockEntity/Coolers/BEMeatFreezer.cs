namespace FoodShelves;

public class BEMeatFreezer : BEBaseFSCooler {
    protected new BlockMeatFreezer block = null!;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    // Base-Specific ----------------------------
    protected override string CantPlaceMessage => "foodshelves:Only raw meat can be placed in this freezer.";
    
    protected override InfoDisplayOptions DisplayInfoOpen => InfoDisplayOptions.BySegment;
    protected override InfoDisplayOptions DisplayInfoClosed => InfoDisplayOptions.AllSegments;
    protected override int DisplayInfoIceIndex => (int)SlotType.IceDrawer;

    public override int ShelfCount => 4;
    public override int AdditionalSlots => 1;

    // Cooler-Specific --------------------------
    public override int CutIceSlot => 4;

    protected override float BuffedPerishMultiplier => 0.65f;
    protected override float UnbuffedPerishMultiplier => 0.65f;

    protected override AssetLocation DoorOpenSound => SoundReferences.MeatFreezerOpen;
    protected override AssetLocation DoorCloseSound => SoundReferences.MeatFreezerClose;
    protected override AssetLocation DrawerOpenSound => SoundReferences.IceDrawerOpen;
    protected override AssetLocation DrawerCloseSound => SoundReferences.IceDrawerClose;
    // ------------------------------------------

    private static readonly string[] iceAnimations = ["iceheight1", "iceheight2", "iceheight3"];
    
    private enum SlotType {
        Segment1 = 0,
        Segment2 = 1,
        Segment3 = 2,
        Segment4 = 3,
        FreezerDoor = 4,
        IceDrawer = 5,
        MeatFreezer = 6
    }

    public BEMeatFreezer() {
        PerishMultiplier = 0.65f; // Needs to be change-able so it's set from within the constructor

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != CutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck, 1, true);
            else return new ItemSlotFSUniversal(inv, FSCoolingOnly, 1, true);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockMeatFreezer)!;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        contentMeshes[0] = GenPartialContentMesh(capi, inv[0], tfMatrices, 0.8f, ShapeReferences.utilMeatFreezer)?.BlockYRotation(block)!;
        contentMeshes[1] = GenPartialContentMesh(capi, inv[1], tfMatrices, 0.8f, ShapeReferences.utilMeatFreezer)?.BlockYRotation(block)!;
        contentMeshes[2] = GenPartialContentMesh(capi, inv[2], tfMatrices, 0.8f, ShapeReferences.utilMeatFreezer)?.BlockYRotation(block)!;
        contentMeshes[3] = GenPartialContentMesh(capi, inv[3], tfMatrices, 0.55f, ShapeReferences.utilMeatFreezer)?.BlockYRotation(block)?.Translate(new(0, 0.25f, 0)).BlockYRotation(block)!;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        
        SlotType aimedAt = (SlotType)blockSel.SelectionBoxIndex;
        bool shift = byPlayer.Entity.Controls.ShiftKey;

        switch (aimedAt) {
            case SlotType.Segment1:
            case SlotType.Segment2:
            case SlotType.Segment3:
            case SlotType.Segment4:
                if (!DoorOpen) return false;
                return base.OnInteract(byPlayer, blockSel);

            case SlotType.FreezerDoor:
                if (!DoorOpen) ToggleDoor(true, byPlayer);
                else ToggleDoor(false, byPlayer);
                MarkDirty(true);
                return true;

            case SlotType.IceDrawer:
                if (shift) {
                    if (!DrawerOpen) ToggleDrawer(true, byPlayer);
                    else ToggleDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }

                if (!slot.Empty) {
                    if (DrawerOpen && slot.CanStoreInSlot(FSCoolingOnly)) {
                        if (TryPutIce(byPlayer, slot, blockSel)) {
                            this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                        }
                    }
                    (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This item cannot be placed in this container."));
                }
                else if (DrawerOpen) {
                    return TryTakeIceOrSlush(byPlayer);
                }
                break;
        }

        return false;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex > (int)SlotType.Segment4)
            return false; // If it's freezer or drawer selection box, return
        
        return base.TryPut(byPlayer, slot, blockSel);
    }

    protected override bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != (int)SlotType.IceDrawer)
            return false;

        return base.TryPutIce(byPlayer, slot, selection);
    }

    #endregion

    #region Animation

    protected override void HandleIceHeight(bool up) {
        if (up) {
            if (inv[CutIceSlot].Itemstack?.StackSize < 20) SetIceHeight(1);
            else if (inv[CutIceSlot].Itemstack?.StackSize < 40) SetIceHeight(2);
            else if (inv[CutIceSlot].Itemstack?.StackSize >= 40) SetIceHeight(3);
        }
        else {
            SetIceHeight(0);
        }
    }

    private void SetIceHeight(int heightLevel) {
        foreach (string anim in iceAnimations) {
            AnimUtil.TryStopAnimation(anim);
        }

        if (heightLevel > 0) {
            SetWaterHeight(false);
        }

        if (heightLevel > 0 && heightLevel <= 3) {
            string animation = "iceheight" + heightLevel;
            float speed = heightLevel switch {
                1 => 3f,
                2 => 8f,
                _ => 6f
            };

            AnimUtil.TryStartAnimation(animation, speed);
        }
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        for (int i = 0; i < 4; i++) {
            if (contentMeshes[i] == null) continue;

            MeshData contentMesh = contentMeshes[i].Clone();
            switch ((BlockDirection)block.GetRotationAngle()) {
                case BlockDirection.North: contentMesh.Translate(i * 0.4375f, 0, 0); break;
                case BlockDirection.West: contentMesh.Translate(0, 0, -i * 0.4375f); break;
                case BlockDirection.South: contentMesh.Translate(-i * 0.4375f, 0, 0); break;
                case BlockDirection.East: contentMesh.Translate(0, 0, i * 0.4375f); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[24][];
        
        float[] x = [  .1f, .22f,  .2f,  .14f, .23f, .57f, .2f, .15f, .6f,  .7f, .65f, .55f, .2f,  .5f,   .1f,   .1f,  .6f, .57f, .1f, .5f,  .1f,  .4f,  .6f, .58f ];
        float[] y = [    0, .01f, .02f,  .03f, .04f, .05f,   0, .06f,   0, .01f,    0, .05f,   0, .01f,  .01f,  .02f, .03f, .04f,   0,   0, .15f, .14f, .15f, .14f ];
        float[] z = [-.05f,  .4f,  .2f, -.07f, .61f,  .3f, .4f, .55f, .1f,  .4f, .62f, .65f, .7f,    0, .075f, .065f,  .6f,  .5f, .5f, .1f, -.1f, .05f,  .4f, .75f ];

        float[] ry = [   0,  -30,    0,    90,   -5,    0,   0,   90,  30,   45,  -10,    5,   2,   90,     5,    -5,   90,    0,   0,   0,    2,   25,   55,   90 ];

        for (int i = 0; i < tfMatrices.Length; i++) {
            tfMatrices[i] = new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY + ry[i])
                .Scale(0.5f, 0.5f, 0.5f)
                .Translate(x[i] * 1.8f - 1.1f, y[i] * 1.8f + 0.5f, z[i] * 1.8f - 1.1f)
                .Values;
        }

        return tfMatrices;
    }
}
