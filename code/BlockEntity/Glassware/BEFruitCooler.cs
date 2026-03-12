namespace FoodShelves;

public class BEFruitCooler : BEBaseFSCooler {
    protected new BlockFruitCooler block = null!;
    private readonly MeshData?[] contentMeshes = new MeshData[4];

    // Base-Specific ----------------------------
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this cooler.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public override int ShelfCount => 4;
    public override int AdditionalSlots => 1;

    // Cooler-Specific --------------------------
    public override int CutIceSlot => 4;

    protected override float BuffedPerishMultiplier => 0.4f;
    protected override float UnbuffedPerishMultiplier => 0.65f;

    protected override AssetLocation DoorOpenSound => SoundReferences.FruitCoolerOpen;
    protected override AssetLocation DoorCloseSound => SoundReferences.FruitCoolerClose;
    protected override AssetLocation DrawerOpenSound => SoundReferences.FruitDrawerOpen;
    protected override AssetLocation DrawerCloseSound => SoundReferences.FruitDrawerClose;

    protected override (string, float) DoorOpenAnim => ("dooropen", 2f);
    protected override (string, float) DrawerOpenAnim => ("draweropen", 4f);
    // ------------------------------------------

    private enum SlotType {
        Segment1 = 0,
        Segment2 = 1,
        Segment3 = 2,
        Segment4 = 3,
        FreezerDoor = 4,
        IceDrawer = 5,
        FruitCooler = 6
    }

    public BEFruitCooler() {
        PerishMultiplier = 0.65f; // Needs to be change-able so it's set from within the constructor

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != CutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck, 1, true);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 1, true);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockFruitCooler)!;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        for (int i = 0; i < 4; i++) {
            contentMeshes[i] = GenLiquidyMesh(capi, inv[i], ShapeReferences.utilFruitCooler, 9f)!.BlockYRotation(block);
        }
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
                    if (DrawerOpen && slot.CanStoreInSlot(CoolingOnly)) {
                        if (TryPutIce(byPlayer, slot, blockSel)) {
                            return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
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
        SetIceHeight(up);
    }

    private void SetIceHeight(bool up) {
        if (up) {
            SetWaterHeight(false);
            AnimUtil.TryStartAnimation("iceup", 6f);
        }
        else {
            AnimUtil.TryStopAnimation("iceup");
        }
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        for (int i = 0; i < 4; i++) {
            if (contentMeshes[i] == null) continue;

            MeshData contentMesh = contentMeshes[i]!.Clone();
            switch ((BlockDirection)block.GetRotationAngle()) {
                case BlockDirection.North: contentMesh.Translate(i%2 * 0.4065f, 0, -i/2 * 0.4065f); break;
                case BlockDirection.West: contentMesh.Translate(-i/2 * 0.4065f, 0, -i%2 * 0.4065f); break;
                case BlockDirection.South: contentMesh.Translate(-i%2 * 0.4065f, 0, i/2 * 0.4065f); break;
                case BlockDirection.East: contentMesh.Translate(i/2 * 0.4065f, 0, i%2 * 0.4065f); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    protected override float[][]? genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        // For ice & water
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.IceDrawer && !inv[CutIceSlot].Empty) {
            if (inv[CutIceSlot].CanStoreInSlot(CoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[CutIceSlot].Itemstack!) + " - " + GetUntilMelted(inv[CutIceSlot]));
            }
            else {
                sb.AppendLine(GetNameAndStackSize(inv[CutIceSlot].Itemstack!));
            }
        }

        // Display all segments if freezer is closed
        if (!DoorOpen && (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.FreezerDoor || forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.FruitCooler)) {
            for (int i = 0; i < 4; i++) {
                if (inv[i * ItemsPerSegment].Empty) {
                    sb.AppendLine(Lang.Get("foodshelves:Empty."));
                }
                else {
                    DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, SlotCount, SegmentsPerShelf, ItemsPerSegment, false, -1, i);
                }
            }
        }
    }
}
