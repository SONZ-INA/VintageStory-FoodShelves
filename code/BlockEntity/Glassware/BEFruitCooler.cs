namespace FoodShelves;

public class BEFruitCooler : BEBaseFSAnimatable {
    protected new BlockFruitCooler block;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this cooler.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool OverrideMergeStacks => true;

    protected override float PerishMultiplier => 0.65f;
    public override int ShelfCount => 4;
    public override int AdditionalSlots => 1;

    [TreeSerializable(false)] public bool CoolerOpen { get; set; }
    [TreeSerializable(false)] public bool DrawerOpen { get; set; }

    private readonly string CoolingOnly = "fsCoolingOnly";
    private float perishMultiplierBuffed = 0.9f;
    private float perishMultiplierUnBuffed = 0.65f;
    public readonly int cutIceSlot = 4;

    public BEFruitCooler() {
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != cutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck, 64);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 64);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockFruitCooler;

        base.Initialize(api);
        
        perishMultiplierBuffed = api.World.Config.GetFloat("FoodShelves.CooledBuff", perishMultiplierBuffed) * perishMultiplierBuffed;
        perishMultiplierUnBuffed = globalBlockBuffs ? perishMultiplierUnBuffed : 1f;

        if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) PerishMultiplier = perishMultiplierBuffed;
        if (CoolerOpen) PerishMultiplier = 1f;
    }

    protected override void InitMesh() {
        base.InitMesh();

        for (int i = 0; i < 4; i++) {
            contentMeshes[i] = GenLiquidyMesh(capi, [inv[i].Itemstack], ShapeReferences.utilFruitCooler, 9f).BlockYRotation(block);
        }
    }

    protected override float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * PerishMultiplier;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[cutIceSlot].Empty && PerishMultiplier < perishMultiplierUnBuffed && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            if (CoolerOpen) PerishMultiplier = 1f;
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
            return (float)((float)1 / inv[cutIceSlot].Itemstack?.StackSize ?? 1) * 5.33f;
        }

        return PerishMultiplier * globalPerishMultiplier;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        switch (blockSel.SelectionBoxIndex) {
            case 0: case 1: case 2: case 3:
                if (!CoolerOpen) return false;
                return base.OnInteract(byPlayer, blockSel);
            case 4:
                if (!CoolerOpen) ToggleFreezerDoor(true, byPlayer);
                else ToggleFreezerDoor(false, byPlayer);
                MarkDirty(true);
                return true;
            case 5:
                if (byPlayer.Entity.Controls.ShiftKey) {
                    if (!DrawerOpen) ToggleFreezerDrawer(true, byPlayer);
                    else ToggleFreezerDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }

                if (!slot.Empty) {
                    if (slot.CanStoreInSlot(CoolingOnly)) {
                        AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                        if (TryPutIce(byPlayer, slot, blockSel)) {
                            Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                            MarkDirty();
                            return true;
                        }
                    }
                    (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This item cannot be placed in this container."));
                }
                else {
                    return TryTakeIceOrSlush(byPlayer);
                }
                break;
            case 6:
                break;
        }

        return false;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex > 3) return false; // If it's freezer or drawer selection box, return
        return base.TryPut(byPlayer, slot, blockSel);
    }

    private bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != 5) return false;
        if (slot.Empty) return false;
        ItemStack stack = inv[cutIceSlot].Itemstack;

        if (inv[cutIceSlot].Empty || (stack.StackSize < stack.Collectible.MaxStackSize && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))) {
            int quantity = byPlayer.Entity.Controls.CtrlKey ? slot.Itemstack.StackSize : 1;
            int moved = slot.TryPutInto(Api.World, inv[cutIceSlot], quantity);

            if (moved == 0 && slot.Itemstack != null) { // Attempt to merge if it fails
                ItemStackMergeOperation op = new(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, quantity) {
                    SourceSlot = new DummySlot(slot.Itemstack),
                    SinkSlot = new DummySlot(stack)
                };
                stack.Collectible.TryMergeStacks(op);
            }

            SetIceHeight(true);
            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
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
            SetIceHeight(false);
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
            if (CoolerOpen) ToggleFreezerDoor(true);
            else ToggleFreezerDoor(false);

            if (DrawerOpen) ToggleFreezerDrawer(true);
            else ToggleFreezerDrawer(false);

            if (!inv[cutIceSlot].Empty) {
                if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                    SetIceHeight(true);
                }
                else {
                    SetWaterHeight(true);
                }
            }
            else {
                SetIceHeight(false);
                SetWaterHeight(false);
            }
        }
    }

    private void ToggleFreezerDoor(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("dooropen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "dooropen",
                    Code = "dooropen",
                    AnimationSpeed = 2f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCoolerOpen, byPlayer.Entity, byPlayer, false, 16, 1f);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("dooropen") == true)
                animUtil.StopAnimation("dooropen");

            PerishMultiplier = perishMultiplierUnBuffed;
            
            if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;
            
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCoolerClose, byPlayer.Entity, byPlayer, false, 16, 1f);
        }

        CoolerOpen = open;
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
                    AnimationSpeed = 4f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerOpen, byPlayer.Entity, byPlayer, true, 16);
            if (!CoolerOpen) PerishMultiplier = perishMultiplierUnBuffed;
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("draweropen") == true) {
                animUtil?.StopAnimation("draweropen");
            }
            if (!CoolerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                PerishMultiplier = perishMultiplierBuffed;
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerClose, byPlayer.Entity, byPlayer, true, 16);
        }

        DrawerOpen = open;
    }

    private void SetIceHeight(bool up) {
        if (up) {
            SetWaterHeight(false);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("iceup") == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = "iceup",
                    Code = "iceup",
                    AnimationSpeed = 6f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("iceup") == true) {
                animUtil?.StopAnimation("iceup");
            }
        }
    }

    private void SetWaterHeight(bool up) {
        if (up) {
            SetIceHeight(false);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterup") == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = "waterup",
                    Code = "waterup",
                    AnimationSpeed = 6f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterup") == true) {
                animUtil?.StopAnimation("waterup");
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
                case 0: contentMesh.Translate(i%2 * 0.4065f, 0, -i/2 * 0.4065f); break;
                case 90: contentMesh.Translate(-i/2 * 0.4065f, 0, -i%2 * 0.4065f); break;
                case 180: contentMesh.Translate(-i%2 * 0.4065f, 0, i/2 * 0.4065f); break;
                case 270: contentMesh.Translate(i/2 * 0.4065f, 0, i%2 * 0.4065f); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        // For ice & water
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == 5 && !inv[cutIceSlot].Empty) {
            if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack) + " - " + GetUntilMelted(inv[cutIceSlot]));
            }
            else {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack));
            }
        }

        // Display all segments if freezer is closed
        if (!CoolerOpen && (forPlayer.CurrentBlockSelection.SelectionBoxIndex == 4 || forPlayer.CurrentBlockSelection.SelectionBoxIndex == 6)) {
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
