namespace FoodShelves;

public abstract class BEBaseFSCooler : BEBaseFSAnimatable {
    public abstract int CutIceSlot { get; }

    protected abstract float BuffedPerishMultiplier { get; }
    protected abstract float UnbuffedPerishMultiplier { get; }

    protected abstract AssetLocation DoorOpenSound { get; }
    protected abstract AssetLocation DoorCloseSound { get; }
    protected abstract AssetLocation DrawerOpenSound { get; }
    protected abstract AssetLocation DrawerCloseSound { get; }

    protected virtual (string, float) DoorOpenAnim => ("dooropen", 3f);
    protected virtual (string, float) DrawerOpenAnim => ("draweropen", 3f);
    protected virtual (string, float) WaterHeightAnim => ("waterheight", 6f);

    [TreeSerializable(false)] public bool DoorOpen { get; set; }
    [TreeSerializable(false)] public bool DrawerOpen { get; set; }

    private float IceMeltRate = 1;
    private float perishMultiplierBuffed;
    private float perishMultiplierUnBuffed;

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);

        IceMeltRate = api.World.Config.GetFloat("FoodShelves.IceMeltRate", IceMeltRate);
        perishMultiplierBuffed = api.World.Config.GetFloat("FoodShelves.CooledBuff", BuffedPerishMultiplier);
        perishMultiplierUnBuffed = globalBlockBuffs ? UnbuffedPerishMultiplier : 1f;

        if (!DrawerOpen && !inv[CutIceSlot].Empty && inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) PerishMultiplier = perishMultiplierBuffed;
        if (DoorOpen) PerishMultiplier = 1f;
    }

    protected override float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * PerishMultiplier;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[CutIceSlot].Empty
            && PerishMultiplier < perishMultiplierUnBuffed
            && !inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)
        ) {
            if (DoorOpen) PerishMultiplier = 1f;
            else PerishMultiplier = perishMultiplierUnBuffed;
            
            SetWaterHeight(true);
            MarkDirty(true);
        }

        if (transType == EnumTransitionType.Dry)
            return container.Room?.ExitCount == 0 ? 2f : 0.5f;

        if (transType == EnumTransitionType.Perish)
            return PerishMultiplier * globalPerishMultiplier;

        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen)
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);

        if (transType == EnumTransitionType.Melt)
            // Single cut ice will last for ~12 hours. However a stack of them will also last ~12 hours, so a multiplier depending on them is needed.
            // A stack should last about 24 days which is 8 ice blocks
            return 1f / (inv[CutIceSlot].Itemstack?.StackSize ?? 1) * 5.33f * IceMeltRate;

        return PerishMultiplier * globalPerishMultiplier;
    }

    #region Interactions

    protected virtual bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (slot.Empty) return false;
        ItemStack? stack = inv[CutIceSlot].Itemstack;

        if (inv[CutIceSlot].Empty || (stack?.StackSize < stack?.Collectible.MaxStackSize && inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly))) {
            int quantity = byPlayer.Entity.Controls.CtrlKey ? slot.Itemstack.StackSize : 1;
            int moved = slot.TryPutIntoBulk(Api.World, inv[CutIceSlot], quantity);

            if (moved > 0) {
                HandleIceHeight(true);
                MarkDirty(true);
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            }

            return moved > 0;
        }

        return false;
    }

    protected virtual bool TryTakeIceOrSlush(IPlayer byPlayer) {
        if (!inv[CutIceSlot].Empty) {
            ItemStack stack = inv[CutIceSlot].TakeOutWhole();
            if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                this.HandlePlacementEffects(stack, byPlayer, true);
            }

            if (stack.StackSize > 0) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            HandleIceHeight(false);
            SetWaterHeight(false);

            return true;
        }

        return false;
    }

    #endregion

    #region Animations

    protected override void HandleAnimations() {
        if (DoorOpen) ToggleDoor(true);
        else ToggleDoor(false);

        if (DrawerOpen) ToggleDrawer(true);
        else ToggleDrawer(false);

        if (!inv[CutIceSlot].Empty) {
            if (inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
                HandleIceHeight(true);
            }
            else {
                SetWaterHeight(true);
            }
        }
        else {
            HandleIceHeight(false);
            SetWaterHeight(false);
        }
    }

    protected virtual void ToggleDoor(bool open, IPlayer? byPlayer = null) {
        if (!inv[CutIceSlot].Empty && !inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (open) {
            AnimUtil.TryStartAnimation(DoorOpenAnim.Item1, DoorOpenAnim.Item2);
            PerishMultiplier = 1f;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(DoorOpenSound, byPlayer, byPlayer, true, 16, 0.3f);
            }
        }
        else {
            AnimUtil.TryStopAnimation(DoorOpenAnim.Item1);
            PerishMultiplier = perishMultiplierUnBuffed;

            if (!DrawerOpen && !inv[CutIceSlot].Empty && inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
                PerishMultiplier = perishMultiplierBuffed;
            }

            if (byPlayer != null) {
                Api.World.PlaySoundAt(DoorCloseSound, byPlayer, byPlayer, true, 16, 0.3f);
            }
        }

        DoorOpen = open;
    }

    protected virtual void ToggleDrawer(bool open, IPlayer? byPlayer = null) {
        if (!inv[CutIceSlot].Empty && !inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (open) {
            AnimUtil.TryStartAnimation(DrawerOpenAnim.Item1, DrawerOpenAnim.Item2);

            if (byPlayer != null) {
                Api.World.PlaySoundAt(DrawerOpenSound, byPlayer, byPlayer, true, 16);
            }

            if (!DoorOpen) {
                PerishMultiplier = perishMultiplierUnBuffed;
            }
        }
        else {
            AnimUtil.TryStopAnimation(DrawerOpenAnim.Item1);

            if (!DoorOpen && !inv[CutIceSlot].Empty && inv[CutIceSlot].CanStoreInSlot(FSCoolingOnly)) {
                PerishMultiplier = perishMultiplierBuffed;
            }

            if (byPlayer != null) {
                Api.World.PlaySoundAt(DrawerCloseSound, byPlayer, byPlayer, true, 16);
            }
        }

        DrawerOpen = open;
    }

    protected abstract void HandleIceHeight(bool up);

    protected virtual void SetWaterHeight(bool up) {
        if (up) {
            HandleIceHeight(false);
            AnimUtil.TryStartAnimation(WaterHeightAnim.Item1, WaterHeightAnim.Item2);
        }
        else {
            AnimUtil.TryStopAnimation(WaterHeightAnim.Item1);
        }
    }

    #endregion
}
