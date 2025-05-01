namespace FoodShelves;

public class BlockEntityGlassJar : BlockEntityDisplay {
    private readonly InventoryGeneric inv;
    private Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    private const int slotCount = 2;
    private float globalPerishMultiplier = 1f;

    public BlockEntityGlassJar() { inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotLiquidyStuff(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);
        globalPerishMultiplier = api.World.Config.GetFloat("FoodShelves.GlobalPerishMultiplier", 1f);

        base.Initialize(api);

        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    internal bool OnInteract(IPlayer byPlayer) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer);
        }
        else {
            if (slot.LiquidyStuffCheck()) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(byPlayer, slot)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }
    }

    // TryPut and TryTake should go to CeilingJar instead of this one, if this one would allow dropoff/pickup of items via UI mouse1/mouse2
    private bool TryPut(IPlayer byPlayer, ItemSlot slot) {
        if (!inv[0].Empty && inv[0].Itemstack.Collectible.Equals(slot.Itemstack.Collectible)) {
            int availableSpaceSlot0 = inv[0].MaxSlotStackSize - inv[0].StackSize;
            int availableSpaceSlot1 = inv[1].MaxSlotStackSize - inv[1].StackSize;

            if (inv[0].StackSize + inv[1].StackSize < inv[0].MaxSlotStackSize + inv[1].MaxSlotStackSize) {
                int moved;
                if (byPlayer.Entity.Controls.ShiftKey) {
                    moved = slot.TryPutInto(Api.World, inv[0], availableSpaceSlot0);

                    if (moved > 0) {
                        int remainingToMove = slot.StackSize;
                        if (remainingToMove > 0) {
                            moved += slot.TryPutInto(Api.World, inv[1], Math.Min(remainingToMove, availableSpaceSlot1));
                        }
                    }
                    else {
                        moved = slot.TryPutInto(Api.World, inv[1], availableSpaceSlot1);
                    }
                }
                else {
                    moved = slot.TryPutInto(Api.World, inv[0], 1);
                    if (moved == 0) moved = slot.TryPutInto(Api.World, inv[1], 1);
                }

                if (moved > 0) {
                    MarkDirty();
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return true;
                }
            }
        }

        if (inv[0].Empty) {
            int moved = slot.TryPutInto(Api.World, inv[0]);

            if (moved > 0) {
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer) {
        ItemStack stack;
        bool overflow = false;

        if (!inv[1].Empty) {
            if (byPlayer.Entity.Controls.ShiftKey) stack = inv[1].TakeOutWhole();
            else stack = inv[1].TakeOut(1);

            if (stack?.StackSize < stack?.Collectible.MaxStackSize && byPlayer.Entity.Controls.ShiftKey) overflow = true;
            if (stack != null && stack.StackSize > 0 && byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                AssetLocation sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                MarkDirty();
                if (!overflow) return true;
            }
        }

        if (!inv[0].Empty || overflow) {
            if (byPlayer.Entity.Controls.ShiftKey) stack = inv[0].TakeOutWhole();
            else stack = inv[0].TakeOut(1);

            if (stack != null && stack.StackSize > 0 && byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                AssetLocation sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                MarkDirty();
                return true;
            }
        }

        return false;
    }

    // Check this method for dehydrated fruit -> dry fruit
    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (Api == null) return 0;

        return baseMul * 0.75f;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        tesselator.TesselateBlock(block, out MeshData blockMesh);
        if (blockMesh == null) return false;

        MeshData contentMesh = GenLiquidyMesh(capi, GetContentStacks(), ShapeReferences.utilGlassJar);
        if (contentMesh != null) blockMesh.AddMeshData(contentMesh);

        mesher.AddMeshData(blockMesh);
        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        DisplayPerishMultiplier(container.GetPerishRate() * globalPerishMultiplier, sb);
        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.ByBlockMerged, slotCount);
    }
}
