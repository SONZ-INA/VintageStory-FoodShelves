namespace FoodShelves;

public class BEMeatFreezer : BEBaseFSAnimatable {
    protected new BlockMeatFreezer block;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    protected override string CantPlaceMessage => "foodshelves:Only raw meat can be placed in this freezer.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool OverrideMergeStacks => true;

    public override int ShelfCount => 4;
    public override int AdditionalSlots => 1;

    [TreeSerializable(false)] public bool FreezerOpen { get; set; }
    [TreeSerializable(false)] public bool DrawerOpen { get; set; }
    
    private readonly string CoolingOnly = "fsCoolingOnly";
    private float IceMeltRate = 1;
    private float perishMultiplierBuffed = 0.6f;
    private float perishMultiplierUnBuffed = 0.65f;
    public readonly int cutIceSlot = 4;

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
            if (id != cutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck, 64);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 64);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockMeatFreezer;

        base.Initialize(api);

        IceMeltRate = api.World.Config.GetFloat("FoodShelves.IceMeltRate", IceMeltRate);
        perishMultiplierBuffed = api.World.Config.GetFloat("FoodShelves.CooledBuff", perishMultiplierBuffed) * perishMultiplierBuffed;
        perishMultiplierUnBuffed = globalBlockBuffs ? perishMultiplierUnBuffed : 1f;

        if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) PerishMultiplier = perishMultiplierBuffed;
        if (FreezerOpen) PerishMultiplier = 1f;
    }

    protected override void InitMesh() {
        base.InitMesh();

        for (int i = 0; i < 3; i++) {
            contentMeshes[i] = GenLiquidyMesh(capi, [inv[i].Itemstack], ShapeReferences.utilMeatFreezer, 13f).BlockYRotation(block);
        }
        contentMeshes[3] = GenLiquidyMesh(capi, [inv[3].Itemstack], ShapeReferences.utilMeatFreezer, 9f)?.Translate(new(0, 0.25f, 0)).BlockYRotation(block);
    }

    protected override float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * PerishMultiplier;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[cutIceSlot].Empty && PerishMultiplier < perishMultiplierUnBuffed && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            if (FreezerOpen) PerishMultiplier = 1f;
            else PerishMultiplier = perishMultiplierUnBuffed;
            SetWaterHeight(true);
            MarkDirty(true);
        }

        if (transType == EnumTransitionType.Dry) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (transType == EnumTransitionType.Perish) return PerishMultiplier * globalPerishMultiplier;

        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        if (transType == EnumTransitionType.Melt) {
            // Single cut ice will last for ~12 hours. However a stack of them will also last ~12 hours, so a multiplier depending on them is needed.
            // A stack should last about 32 days which is 8 ice blocks
            return (float)((float)1 / inv[cutIceSlot].Itemstack?.StackSize ?? 1) * 5.33f * IceMeltRate;
        }

        return PerishMultiplier * globalPerishMultiplier;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        
        SlotType aimedAt = (SlotType)blockSel.SelectionBoxIndex;

        switch (aimedAt) {
            case SlotType.Segment1: case SlotType.Segment2: case SlotType.Segment3: case SlotType.Segment4:
                if (!FreezerOpen) return false;
                return base.OnInteract(byPlayer, blockSel);

            case SlotType.FreezerDoor:
                if (!FreezerOpen) ToggleFreezerDoor(true, byPlayer);
                else ToggleFreezerDoor(false, byPlayer);
                MarkDirty(true);
                return true;

            case SlotType.IceDrawer:
                if (byPlayer.Entity.Controls.ShiftKey) {
                    if (!DrawerOpen) ToggleFreezerDrawer(true, byPlayer);
                    else ToggleFreezerDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }

                if (!slot.Empty) {
                    if (DrawerOpen && slot.CanStoreInSlot(CoolingOnly)) {
                        AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                        if (TryPutIce(byPlayer, slot, blockSel)) {
                            Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                            MarkDirty();
                            return true;
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
        if (blockSel.SelectionBoxIndex > (int)SlotType.Segment4) return false; // If it's freezer or drawer selection box, return
        return base.TryPut(byPlayer, slot, blockSel);
    }

    private bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != (int)SlotType.IceDrawer) return false;
        if (slot.Empty) return false;
        ItemStack stack = inv[cutIceSlot].Itemstack;

        if (inv[cutIceSlot].Empty || (stack.StackSize < stack.Collectible.MaxStackSize && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))) {
            int quantity = byPlayer.Entity.Controls.CtrlKey ? slot.Itemstack.StackSize : 1;
            int moved = slot.TryPutInto(Api.World, inv[cutIceSlot], quantity);

            if (moved == 0 && slot.Itemstack != null) { // Attempt to merge if it fails
                ItemStackMergeOperation op = new(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.ConfirmedMerge, quantity) {
                    SourceSlot = new DummySlot(slot.Itemstack),
                    SinkSlot = new DummySlot(stack)
                };
                stack.Collectible.TryMergeStacks(op);
            }

            if (inv[cutIceSlot].Itemstack?.StackSize < 20) SetIceHeight(1);
            else if (inv[cutIceSlot].Itemstack?.StackSize < 40) SetIceHeight(2);
            else if (inv[cutIceSlot].Itemstack?.StackSize >= 40) SetIceHeight(3);

            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return true;
        }

        return false;
    }

    private bool TryTakeIceOrSlush(IPlayer byPlayer) {
        if (!inv[cutIceSlot].Empty) {
            ItemStack stack = inv[cutIceSlot].TakeOutWhole();
            if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                AssetLocation sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
            }

            if (stack.StackSize > 0) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            SetIceHeight(0);
            SetWaterHeight(false);

            MarkDirty(true);
            return true;
        }

        return false;
    }

    #endregion

    #region Animation

    protected override void HandleAnimations() {
        if (animUtil != null) {
            if (FreezerOpen) ToggleFreezerDoor(true);
            else ToggleFreezerDoor(false);

            if (DrawerOpen) ToggleFreezerDrawer(true);
            else ToggleFreezerDrawer(false);

            if (!inv[cutIceSlot].Empty) {
                if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                    if (inv[cutIceSlot].Itemstack?.StackSize < 20) SetIceHeight(1);
                    else if (inv[cutIceSlot].Itemstack?.StackSize < 40) SetIceHeight(2);
                    else if (inv[cutIceSlot].Itemstack?.StackSize >= 40) SetIceHeight(3);
                }
                else {
                    SetWaterHeight(true);
                }
            }
            else {
                SetIceHeight(0);
                SetWaterHeight(false);
            }
        }
    }

    private void ToggleFreezerDoor(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("freezeropen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "freezeropen",
                    Code = "freezeropen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundFreezerOpen, byPlayer.Entity, byPlayer, true, 16, 0.3f);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("freezeropen") == true)
                animUtil.StopAnimation("freezeropen");

            PerishMultiplier = perishMultiplierUnBuffed;
            
            if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;
            
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundFreezerClose, byPlayer.Entity, byPlayer, true, 16, 0.3f);
        }

        FreezerOpen = open;
    }

    private void ToggleFreezerDrawer(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }


        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("draweropen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "draweropen",
                    Code = "draweropen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerOpen, byPlayer.Entity, byPlayer, true, 16);
            if (!FreezerOpen) PerishMultiplier = perishMultiplierUnBuffed;
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("draweropen") == true) {
                animUtil?.StopAnimation("draweropen");
            }
            if (!FreezerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                PerishMultiplier = perishMultiplierBuffed;
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerClose, byPlayer.Entity, byPlayer, true, 16);
        }

        DrawerOpen = open;
    }

    private void SetIceHeight(int heightLevel) {
        string[] iceAnimations = ["iceheight1", "iceheight2", "iceheight3"];

        foreach (string anim in iceAnimations) {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey(anim) == true) {
                animUtil?.StopAnimation(anim);
            }
        }

        if (heightLevel > 0) {
            SetWaterHeight(false);
        }

        if (heightLevel > 0 && heightLevel <= 3) {
            string animation = "iceheight" + heightLevel;
            float speed = heightLevel == 1 ? 3f : (heightLevel == 2 ? 8f : 6f);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey(animation) == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = animation,
                    Code = animation,
                    AnimationSpeed = speed,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
    }

    private void SetWaterHeight(bool up) {
        if (up) {
            SetIceHeight(0);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterheight") == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = "waterheight",
                    Code = "waterheight",
                    AnimationSpeed = 6f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterheight") == true) {
                animUtil?.StopAnimation("waterheight");
            }
        }
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        for (int i = 0; i < 4; i++) {
            if (contentMeshes[i] == null) continue;

            MeshData contentMesh = contentMeshes[i].Clone();
            switch (block.GetRotationAngle()) {
                case 0: contentMesh.Translate(i * 0.4375f, 0, 0); break;
                case 90: contentMesh.Translate(0, 0, -i * 0.4375f); break;
                case 180: contentMesh.Translate(-i * 0.4375f, 0, 0); break;
                case 270: contentMesh.Translate(0, 0, i * 0.4375f); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        // For ice & water
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.IceDrawer && !inv[cutIceSlot].Empty) {
            if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack) + " - " + GetUntilMelted(inv[cutIceSlot]));
            }
            else {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack));
            }
        }

        // Display all segments if freezer is closed
        if (!FreezerOpen && (forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.FreezerDoor || forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.MeatFreezer)) {
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
