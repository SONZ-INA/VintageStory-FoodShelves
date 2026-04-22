namespace FoodShelves;

public class BECoolingCabinet : BEBaseFSCooler {
    protected new BlockCoolingCabinet block = null!;

    // Base-Specific ----------------------------
    public override string AttributeTransformCode => "onHolderUniversalTransform";
    public override string AttributeCheck => "fsHolderUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;
    public override int SegmentsPerShelf => 3;
    public override int ItemsPerSegment => 24;
    public override int AdditionalSlots => 1;

    // Cooler-Specific --------------------------
    public override int CutIceSlot => 216;

    protected override float BuffedPerishMultiplier => 0.2985f;
    protected override float UnbuffedPerishMultiplier => 0.74f;

    protected override AssetLocation DoorOpenSound => SoundReferences.CoolingCabinetOpen;
    protected override AssetLocation DoorCloseSound => SoundReferences.CoolingCabinetClose;
    protected override AssetLocation DrawerOpenSound => SoundReferences.IceDrawerOpen;
    protected override AssetLocation DrawerCloseSound => SoundReferences.IceDrawerClose;
    // ------------------------------------------

    private static readonly string[] iceAnimations = ["iceheight1", "iceheight2", "iceheight3"];
    
    private enum SlotType {
        Segments = 8,
        LDoor = 9,
        RDoor = 10,
        IceDrawer = 11,
        ClosedCabinet = 12
    }

    public BECoolingCabinet() {
        PerishMultiplier = 0.75f; // Needs to be change-able so it's set from within the constructor

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != CutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck);
            else return new ItemSlotFSUniversal(inv, FSCoolingOnly, 1, true);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockCoolingCabinet)!;
        base.Initialize(api);
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool shift = byPlayer.Entity.Controls.ShiftKey;
        bool ctrl = byPlayer.Entity.Controls.CtrlKey;

        // Open/Close cabinet or drawer
        switch ((SlotType)blockSel.SelectionBoxIndex) {
            case SlotType.IceDrawer:
                if (shift) {
                    if (!DrawerOpen) ToggleDrawer(true, byPlayer);
                    else ToggleDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }
                break;

            case SlotType.ClosedCabinet:
                ToggleDoor(true, byPlayer);
                MarkDirty(true);
                return true;

            case SlotType.LDoor:
            case SlotType.RDoor:
                ToggleDoor(false, byPlayer);
                MarkDirty(true);
                return true;
        }

        // Take/Put items
        if (slot.Empty) {
            if (DoorOpen && blockSel.SelectionBoxIndex <= (int)SlotType.Segments) {
                // In-container interactions
                if (ctrl && TryUse(byPlayer, slot, blockSel))
                    return true;

                return TryTake(byPlayer, blockSel);
            }
            else if (DrawerOpen && blockSel.SelectionBoxIndex == (int)SlotType.IceDrawer) {
                return TryTakeIceOrSlush(byPlayer);
            }

            return false;
        }
        else {
            // In-container interactions
            if (DoorOpen && TryUse(byPlayer, slot, blockSel))
                return true;

            if (DoorOpen && slot.CanStoreInSlot(AttributeCheck)) {
                if (TryPut(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }

            if (DrawerOpen && slot.CanStoreInSlot(FSCoolingOnly)) {
                if (TryPutIce(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }

            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This item cannot be placed in this container."));
            return false;
        }
    }

    protected bool TryUse(IPlayer player, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex > (int)SlotType.Segments) return false; // If it's cabinet or drawer selection box, return

        int segmentIndex = blockSel.SelectionBoxIndex;
        int startIndex = segmentIndex * ItemsPerSegment;
        int endIndex = startIndex + ItemsPerSegment - 20; // Offset of 20 since the crocks can only fit 4 in a segment.

        // If it's empty, shift the check further down - crocks in the back can be reached.
        if (inv[endIndex - 1].Empty) endIndex--;
        if (inv[endIndex - 1].Empty) endIndex--;

        if (inv[startIndex].Itemstack?.Collectible is BaseFSBasket && inv[startIndex].Itemstack?.Collectible is IContainedInteractable ic)
            return ic.OnContainedInteractStart(this, inv[startIndex], player, blockSel);

        // Only check last 2 slots (visually front crocks)
        for (int i = endIndex - 1; i >= Math.Max(startIndex, endIndex - 2); i--) {
            var stack = inv[i].Itemstack;
            var stackSize = slot.Itemstack?.StackSize ?? 0;

            if (stack?.Collectible is IContainedInteractable ici && ici.OnContainedInteractStart(this, inv[i], player, blockSel)) {
                // If it's a meal container, don't check for "sealing" behavior
                if (slot.Itemstack?.ItemAttributes["mealContainer"].AsBool() == true) {
                    MarkDirty();
                    return true;
                }

                // If item is consumed for sealing, stop.
                int afterSize = slot.Itemstack?.StackSize ?? 0;
                if (stackSize != afterSize) {
                    MarkDirty();
                    return true;
                }
                // Otherwise, keep looping to check the crock behind
            }
        }

        return false;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int segmentIndex = blockSel.SelectionBoxIndex;
        if (segmentIndex > (int)SlotType.Segments)
            return false;

        return base.TryPut(byPlayer, slot, blockSel);
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        startIndex *= ItemsPerSegment;

        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int currentIndex = startIndex + i;
            if (!inv[currentIndex].Empty) {
                ItemStack stack = inv[currentIndex].TakeOut(1);
                
                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    this.HandlePlacementEffects(stack, byPlayer);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                return true;
            }
        }

        return false;
    }

    protected override int GetSegmentLimit(ItemStack? stack) {
        return SegmentLimits.Mixed(this, stack);
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

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.x = td.segment * 0.625f - 0.125f;
            td.y = td.shelf * 0.47f + 0.625f;

            td.scaleX = td.scaleY = td.scaleZ = 0.95f;
        }, true);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        // For ice & water
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.IceDrawer && !inv[CutIceSlot].Empty) {
            if (inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[CutIceSlot].Itemstack!) + " - " + TransitionInfoCompact(Api.World, inv[CutIceSlot], EnumTransitionType.Melt, TransitionDisplayMode.TimeLeft));
            }
            else {
                sb.AppendLine(GetNameAndStackSize(inv[CutIceSlot].Itemstack!));
            }
        }

        // Cycle segments when cabinet is closed
        if (!DoorOpen && forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.ClosedCabinet) {
            int currentSegment = (int)(Api.World.ElapsedMilliseconds / 2000) % 9;
            sb.AppendLine(Lang.Get("foodshelves:Displaying segment") + " " + Lang.Get("foodshelves:segmentnum-" + currentSegment));

            if (inv[currentSegment * ItemsPerSegment].Empty) {
                sb.AppendLine(Lang.Get("foodshelves:Empty."));
            }
            else {
                DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, SlotCount, SegmentsPerShelf, ItemsPerSegment, false, -1, currentSegment);
            }
        }
    }
}
