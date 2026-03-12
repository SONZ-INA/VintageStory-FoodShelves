using Vintagestory.API.Server;

namespace FoodShelves;

public abstract class BEBaseFSContainer : BlockEntityDisplay, IFoodShelvesContainer {
    protected float globalPerishMultiplier = 1f;
    protected bool globalBlockBuffs = true;

    public InventoryGeneric inv = null!;
    protected BaseFSContainer block = null!;
    protected MeshData? blockMesh;

    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Code.FirstCodePart() ?? "-temp-";
    public override string AttributeTransformCode => "on" + Block?.Code.FirstCodePart() ?? "-temp-" + "Transform";

    public ITreeAttribute VariantAttributes { get; set; } = new TreeAttribute();
    public virtual string AttributeCheck => "fs" + GetType().Name.Replace("BE", "");

    protected virtual string CantPlaceMessage => "";
    protected abstract InfoDisplayOptions InfoDisplay { get; }
    protected virtual bool RipeningSpot => false;

    protected virtual float PerishMultiplier { get; set; } = 1;
    protected virtual float CuringMultiplier { get; set; } = 1;
    protected virtual float DryingMultiplier { get; set; } = 1;

    public virtual int ShelfCount { get; set; } = 1;
    public virtual int SegmentsPerShelf { get; set; } = 1;
    public virtual int ItemsPerSegment { get; set; } = 1;
    public virtual int AdditionalSlots { get; set; } = 0;
    public virtual int SlotCount => ShelfCount * SegmentsPerShelf * ItemsPerSegment + AdditionalSlots;

    public override void Initialize(ICoreAPI api) {
        block ??= (api.World.BlockAccessor.GetBlock(Pos) as BaseFSContainer)!;
        globalPerishMultiplier = api.World.Config.GetFloat("FoodShelves.GlobalPerishMultiplier", 1f);
        globalBlockBuffs = api.World.Config.GetBool("FoodShelves.GlobalBlockBuffs", true);

        base.Initialize(api);

        if (blockMesh == null) InitMesh();
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    protected virtual void InitMesh() {
        blockMesh = GenBlockVariantMesh(Api, this.GetVariantStack());
    }

    public override void OnBlockPlaced(ItemStack byItemStack) {
        base.OnBlockPlaced(byItemStack);

        if (byItemStack?.Attributes[FSAttributes] is ITreeAttribute tree) {
            if (VariantAttributes.Count == 0) VariantAttributes = tree;
        }
        
        InitMesh();
    }

    protected virtual float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * (globalBlockBuffs ? PerishMultiplier : 1);
    }

    public virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) {
            if (!globalBlockBuffs) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
            return container.Room?.ExitCount == 0 ? DryingMultiplier * 2f : DryingMultiplier * 0.5f;
        }

        if (transType == EnumTransitionType.Cure)
            return globalBlockBuffs ? CuringMultiplier : 1f;

        if (Api == null) return 0;

        if (RipeningSpot && transType == EnumTransitionType.Ripen)
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);

        return globalPerishMultiplier * (globalBlockBuffs ? PerishMultiplier : 1);
    }

    public virtual bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool shift = byPlayer.Entity.Controls.ShiftKey;
        bool isBulk = block.interactionType == BlockInteractionType.Bulk;

        bool placeBulk = isBulk && shift;
        bool placeSingle = !isBulk && !shift && !slot.Empty;

        if (placeBulk || placeSingle) {
            if (slot.Empty) return false;

            if (slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck)) {
                if (TryPut(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }

            if (CantPlaceMessage != "") {
                (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get(CantPlaceMessage));
            }

            return false;
        }

        return TryTake(byPlayer, blockSel);
    }

    protected virtual bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;
        if (startIndex >= inv.Count) return false;

        bool ctrl = byPlayer.Entity.Controls.CtrlKey;
        int moved = 0;

        for (int i = 0; i < ItemsPerSegment; i++) {
            int idx = startIndex + i;
            ItemSlot target = inv[idx];
            ItemStack stack = target.Itemstack!;

            if (target.Empty || stack.Collectible == slot.Itemstack!.Collectible) {
                ItemSlotFSUniversal fsSlot = (inv[idx] as ItemSlotFSUniversal)!;
                int availableSpace = fsSlot.GetRemainingSlotSpace(slot.Itemstack!);
                if (availableSpace == 0) return false;

                moved = slot.TryPutIntoBulk(Api.World, inv[idx], ctrl ? availableSpace : 1);

                // Only break if something was actually moved
                if (moved > 0) break;
            }
        }

        if (moved > 0) {
            InitMesh();
            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            return true;
        }

        return false;
    }

    protected virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;
        if (startIndex >= inv.Count) return false;

        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int currentIndex = startIndex + i;
            if (!inv[currentIndex].Empty) {
                ItemStack stack = byPlayer.Entity.Controls.CtrlKey
                    ? inv[currentIndex].TakeOut(inv[currentIndex].Itemstack!.Collectible.MaxStackSize)
                    : inv[currentIndex].TakeOut(1);

                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    this.HandlePlacementEffects(stack, byPlayer);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                InitMesh();
                return true;
            }
        }

        return false;
    }

    protected virtual bool TryTakeFromSlot(IPlayer byPlayer, ItemSlot slot, int quantity = 1) {
        if (!slot.Empty) {
            ItemStack stack = slot.TakeOut(quantity);

            if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                this.HandlePlacementEffects(stack, byPlayer);
            }

            if (stack.StackSize > 0) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            InitMesh();
            return true;
        }

        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        mesher.AddMeshData(blockMesh);
        base.OnTesselation(mesher, tesselator);
        return true;
    }

    protected virtual bool BaseRenderContents(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        return base.OnTesselation(mesher, tesselator);
    }

    protected abstract override float[][]? genTransformationMatrices();

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);

        VariantAttributes = tree[FSAttributes] is ITreeAttribute fsTree 
            ? fsTree 
            : new TreeAttribute();

        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        if (VariantAttributes.Count != 0) {
            tree[FSAttributes] = VariantAttributes;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        DisplayPerishMultiplier(GetPerishRate(), sb);

        if (RipeningSpot) {
            float ripenRate = GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
            if (ripenRate > 0) sb.Append(Lang.Get("Suitable spot for food ripening."));
        }

        DisplayInfo(forPlayer, sb, inv, InfoDisplay, SlotCount, SegmentsPerShelf, ItemsPerSegment, true, SlotCount - AdditionalSlots);
    }
}
