namespace FoodShelves;

public class BECoolingCabinet : BEBaseFSAnimatable {
    protected new BlockCoolingCabinet block;

    public override string AttributeTransformCode => "onHolderUniversalTransform";
    public override string AttributeCheck => "fsHolderUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;
    public override int SegmentsPerShelf => 3;
    public override int ItemsPerSegment => 24;
    public override int AdditionalSlots => 1;

    [TreeSerializable(false)] public bool CabinetOpen { get; set; }
    [TreeSerializable(false)] public bool DrawerOpen { get; set; }

    private readonly string CoolingOnly = "fsCoolingOnly";
    private float perishMultiplierBuffed = 0.3f;
    private float perishMultiplierUnBuffed = 0.75f;
    public readonly int cutIceSlot = 216;

    private enum SlotType {
        Segments = 8,
        IceDrawer = 9,
        ClosedCabinet = 10
    }

    public BECoolingCabinet() {
        PerishMultiplier = 0.75f; // Needs to be change-able so it's set from within the constructor

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != cutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 64);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockCoolingCabinet;

        base.Initialize(api);
        
        perishMultiplierBuffed = api.World.Config.GetFloat("FoodShelves.CooledBuff", perishMultiplierBuffed);
        perishMultiplierUnBuffed = globalBlockBuffs ? 0.75f : 1f;

        if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) PerishMultiplier = perishMultiplierBuffed;
        if (CabinetOpen) PerishMultiplier = 1f;
    }

    protected override float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * PerishMultiplier;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[cutIceSlot].Empty && PerishMultiplier < perishMultiplierUnBuffed && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            if (CabinetOpen) PerishMultiplier = 1f;
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
            // A stack should last about 24 days which is 8 ice blocks
            return (float)((float)1 / inv[cutIceSlot].Itemstack?.StackSize ?? 1) * 5.33f;
        }

        return PerishMultiplier * globalPerishMultiplier;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool shift = byPlayer.Entity.Controls.ShiftKey;
        bool ctrl = byPlayer.Entity.Controls.CtrlKey;

        // Open/Close cabinet or drawer
        if (shift) {
            switch (blockSel.SelectionBoxIndex) {
                case (int)SlotType.IceDrawer:
                    if (!DrawerOpen) ToggleCabinetDrawer(true, byPlayer);
                    else ToggleCabinetDrawer(false, byPlayer);
                    break;

                default:
                    if (!CabinetOpen) ToggleCabinetDoor(true, byPlayer);
                    else ToggleCabinetDoor(false, byPlayer);
                    break;
            }

            MarkDirty(true);
            return true;
        }

        // Take/Put items
        if (slot.Empty) {
            if (CabinetOpen && blockSel.SelectionBoxIndex <= (int)SlotType.Segments) {
                // In-container interactions
                if (ctrl && TryUse(byPlayer, slot, blockSel)) {
                    return true;
                }

                return TryTake(byPlayer, blockSel);
            }
            else if (DrawerOpen && blockSel.SelectionBoxIndex == (int)SlotType.IceDrawer) {
                return TryTakeIceOrSlush(byPlayer);
            }

            return false;
        }
        else {
            // In-container interactions
            if (CabinetOpen && TryUse(byPlayer, slot, blockSel)) { 
                return true;
            }

            if (CabinetOpen && slot.CanStoreInSlot(AttributeCheck)) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(byPlayer, slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            if (DrawerOpen && slot.CanStoreInSlot(CoolingOnly)) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPutIce(byPlayer, slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
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

        if (inv[startIndex].Itemstack?.Collectible is BaseFSBasket && inv[startIndex].Itemstack?.Collectible is IContainedInteractable ic) {
            return ic.OnContainedInteractStart(this, inv[startIndex], player, blockSel);
        }

        // Only check last 2 slots (visually front crocks)
        for (int i = endIndex - 1; i >= Math.Max(startIndex, endIndex - 2); i--) {
            var stack = inv[i]?.Itemstack;
            var stackSize = slot?.Itemstack?.StackSize ?? 0;

            if (stack?.Collectible is IContainedInteractable ici && ici.OnContainedInteractStart(this, inv[i], player, blockSel)) {
                // If it's a meal container, don't check for "sealing" behavior
                if (slot?.Itemstack?.ItemAttributes["mealContainer"].AsBool() == true) {
                    MarkDirty();
                    return true;
                }

                // If item is consumed for sealing, stop.
                int afterSize = slot?.Itemstack?.StackSize ?? 0;
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
        int startIndex = blockSel.SelectionBoxIndex;
        if (startIndex > (int)SlotType.Segments) return false; // If it's cabinet or drawer selection box, return

        ItemStack stack = slot.Itemstack;
        startIndex *= ItemsPerSegment;

        if (!inv[startIndex].Empty) {
            ItemStack firstItemOnSegment = inv[startIndex].Itemstack;
            if (!firstItemOnSegment.BelongsToSameGroupAs(stack)) return false;
            if (stack.IsLargeItem() || firstItemOnSegment.IsLargeItem()) return false;
            if (firstItemOnSegment.IsSmallItem() != stack.IsSmallItem()) return false; 
        }

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (currentIndex == startIndex + 4 && !stack.IsSmallItem()) 
                return false;

            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        startIndex *= ItemsPerSegment;

        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int currentIndex = startIndex + i;
            if (!inv[currentIndex].Empty) {
                ItemStack stack = inv[currentIndex].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();
                return true;
            }
        }

        return false;
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
            if (CabinetOpen) ToggleCabinetDoor(true);
            else ToggleCabinetDoor(false);

            if (DrawerOpen) ToggleCabinetDrawer(true);
            else ToggleCabinetDrawer(false);

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

    private void ToggleCabinetDoor(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }


        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "cabinetopen",
                    Code = "cabinetopen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetOpen, byPlayer.Entity, byPlayer, true, 16, 0.3f);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == true)
                animUtil.StopAnimation("cabinetopen");

            PerishMultiplier = perishMultiplierUnBuffed;
            
            if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;
            
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetClose, byPlayer.Entity, byPlayer, true, 16, 0.3f);
        }

        CabinetOpen = open;
    }

    private void ToggleCabinetDrawer(bool open, IPlayer byPlayer = null) {
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
            if (!CabinetOpen) PerishMultiplier = perishMultiplierUnBuffed;
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("draweropen") == true)
                animUtil?.StopAnimation("draweropen");

            if (!CabinetOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;

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

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;
                    if (inv[index].Empty) {
                        tfMatrices[index] = new Matrixf().Values;
                        continue;
                    }

                    var itemStack = inv[index].Itemstack;

                    float x, y = shelf * 0.4921875f, z;
                    float scale = 0.95f;

                    if (itemStack.IsLargeItem()) {
                        x = segment * 0.65f;
                        z = item * 0.65f;
                    }
                    else if (!itemStack.IsSmallItem()) {
                        x = segment * 0.65f + (index % 2 == 0 ? -0.16f : 0.16f);
                        z = (index / 2) % 2 == 0 ? -0.18f : 0.18f;
                    }
                    else {
                        x = segment * 0.763f + (item % 4) * 0.19f - 0.314f;
                        y = y * 1.16f + (item / 8) * 0.10f + 0.103f;
                        z = ((item / 4) % 2) * 0.45f - 0.25f;
                        scale = 0.82f;
                    }

                    // Exceptions I have to hardcode -----
                    if (!itemStack.IsLargeItem()) {
                        string itemPath = itemStack.Collectible.Code.Path;

                        if (itemPath.Contains("pie") == true || itemPath.Contains("cheese")) {
                            x += 0.15f;
                            z += 0.1f;
                        }
                    }

                    string[] collectibleCodes = ["pemmican-pack", "chips-pack", "mushroom-pack"];
                    if (collectibleCodes.Contains(itemStack.Collectible.Code.Path)) {
                        y += item / 2 * 0.13f;
                        z = -0.18f;
                    }
                    // -----------------------------------

                    tfMatrices[index] = new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(scale, scale, scale)
                        .Translate(x - 0.625f, y + 0.66f, z - 0.5325f)
                        .Values;
                }
            }
        }

        tfMatrices[cutIceSlot] = new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values; // Hide original cut ice shape, can't bother to custom mesh it out

        return tfMatrices;
    }

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

        // Cycle segments when cabinet is closed
        if (!CabinetOpen && forPlayer.CurrentBlockSelection.SelectionBoxIndex == (int)SlotType.ClosedCabinet) {
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
