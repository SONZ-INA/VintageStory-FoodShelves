namespace FoodShelves;

public class BEMeatFreezer : BEBaseFSCooler {
    protected new BlockMeatFreezer block = null!;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    // Base-Specific ----------------------------
    protected override string CantPlaceMessage => "foodshelves:Only raw meat can be placed in this freezer.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool OverrideMergeStacks => true;

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
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 1, true);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockMeatFreezer)!;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        contentMeshes[0] = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilMeatFreezer, 13f)?.BlockYRotation(block)!;
        contentMeshes[1] = GenLiquidyMesh(capi, inv[1], ShapeReferences.utilMeatFreezer, 13f)?.BlockYRotation(block)!;
        contentMeshes[2] = GenLiquidyMesh(capi, inv[2], ShapeReferences.utilMeatFreezer, 13f)?.BlockYRotation(block)!;
        contentMeshes[3] = GenLiquidyMesh(capi, inv[3], ShapeReferences.utilMeatFreezer, 9f)?.Translate(new(0, 0.25f, 0)).BlockYRotation(block)!;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        
        SlotType aimedAt = (SlotType)blockSel.SelectionBoxIndex;

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
                if (byPlayer.Entity.Controls.ShiftKey) {
                    if (!DrawerOpen) ToggleDrawer(true, byPlayer);
                    else ToggleDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }

                if (!slot.Empty) {
                    if (DrawerOpen && slot.CanStoreInSlot(CoolingOnly)) {
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
        if (!DoorOpen && (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.FreezerDoor || forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.MeatFreezer)) {
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
