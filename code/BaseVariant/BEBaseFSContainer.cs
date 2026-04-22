namespace FoodShelves;

public abstract class BEBaseFSContainer : BlockEntityDisplay, IFoodShelvesContainer {
    protected float globalPerishMultiplier = 1f;
    protected bool globalBlockBuffs = true;

    public InventoryGeneric inv = null!;
    
    protected BaseFSContainer block = null!;
    protected MeshData? blockMesh;

    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => (Block?.Code.FirstCodePart() ?? "-temp-");
    public override string AttributeTransformCode => "on" + (Block?.Code.FirstCodePart() ?? "-temp-") + "Transform";

    public ITreeAttribute VariantAttributes { get; set; } = new TreeAttribute();
    public virtual string AttributeCheck => "fs" + GetType().Name.Replace("BE", "");

    protected abstract InfoDisplayOptions InfoDisplay { get; }
    
    protected virtual string CantPlaceMessage => "";
    protected virtual bool RipeningSpot => false;

    protected virtual float PerishMultiplier { get; set; } = 1;
    protected virtual float CuringMultiplier { get; set; } = 1;
    protected virtual float DryingMultiplier { get; set; } = 1;

    public virtual int ShelfCount { get; set; } = 1;
    public virtual int SegmentsPerShelf { get; set; } = 1;
    public virtual int ItemsPerSegment { get; set; } = 1;
    public virtual int AdditionalSlots { get; set; } = 0;
    public virtual int SlotCount => ShelfCount * SegmentsPerShelf * ItemsPerSegment + AdditionalSlots;

    protected bool isBulk = false;

    public override void Initialize(ICoreAPI api) {
        block ??= (api.World.BlockAccessor.GetBlock(Pos) as BaseFSContainer)!;
        globalPerishMultiplier = api.World.Config.GetFloat("FoodShelves.GlobalPerishMultiplier", 1f);
        globalBlockBuffs = api.World.Config.GetBool("FoodShelves.GlobalBlockBuffs", true);

        base.Initialize(api);

        if (blockMesh == null) InitMesh();
        
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;

        // Restrict chutes from interacting
        inv.OnGetAutoPushIntoSlot = (_, _) => null;
        inv.OnGetAutoPullFromSlot = _ => null;

        foreach (var inv in Inventory) {
            ItemSlotFSUniversal? fsSlot = inv as ItemSlotFSUniversal;
            if (fsSlot?.isBulk == true) {
                isBulk = true;
                break;
            }
        }
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
        int segmentIndex = blockSel.SelectionBoxIndex;

        int startIndex = segmentIndex * ItemsPerSegment;
        if (startIndex >= inv.Count) return false;

        ItemStack incoming = slot.Itemstack!;

        if (!CanInsertIntoSegment(inv[startIndex].Itemstack, incoming))
            return false;

        if (!isBulk) {
            int limit = GetSegmentLimit(incoming);
            int count = CountItemsInSegment(startIndex);

            if (count >= limit)
                return false;
        }

        bool ctrl = byPlayer.Entity.Controls.CtrlKey;
        int moved = TryPutIntoSegment(slot, startIndex, ctrl);

        if (moved > 0) {
            InitMesh();
            MarkDirty();
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            return true;
        }

        return false;
    }

    protected virtual int TryPutIntoSegment(ItemSlot slot, int startIndex, bool ctrl) {
        int moved = 0;

        for (int i = 0; i < ItemsPerSegment; i++) {
            int idx = startIndex + i;
            ItemSlot target = inv[idx];

            if (target.Empty || target.Itemstack!.Collectible == slot.Itemstack!.Collectible) {
                var fsSlot = (ItemSlotFSUniversal)target;
                int available = fsSlot.GetRemainingSlotSpace(slot.Itemstack!);
                if (available == 0) continue;

                moved = slot.TryPutIntoBulk(Api.World, target, ctrl ? available : 1);
                if (moved > 0) {
                    if (moved <= slot.StackSize && ctrl) {
                        continue;
                    }

                    break;
                }
            }
        }

        return moved;
    }

    protected virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;
        if (startIndex >= inv.Count) return false;

        ItemStack? stack = TryTakeFromSegment(byPlayer, startIndex);
        if (stack == null) return false;

        if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
            this.HandlePlacementEffects(stack, byPlayer);
        }

        if (stack.StackSize > 0) {
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        InitMesh();
        return true;
    }

    protected virtual ItemStack? TryTakeFromSegment(IPlayer byPlayer, int startIndex) {
        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int idx = startIndex + i;

            if (!inv[idx].Empty) {
                return byPlayer.Entity.Controls.CtrlKey
                    ? inv[idx].TakeOut(inv[idx].Itemstack!.Collectible.MaxStackSize)
                    : inv[idx].TakeOut(1);
            }
        }

        return null;
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

    protected virtual int GetSegmentLimit(ItemStack? stack) {
        return ItemsPerSegment;
    }

    public virtual int CountItemsInSegment(int startIndex) {
        int count = 0;

        for (int i = 0; i < ItemsPerSegment; i++) {
            if (!inv[startIndex + i].Empty) {
                count++;
            }
        }

        return count;
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
