namespace FoodShelves;

public class BlockEntityCeilingJar : BlockEntityDisplay {
    private readonly InventoryGeneric inv;
    private Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    private const int slotCount = 12;

    public BlockEntityCeilingJar() { inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotLiquidyStuff(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);
        
        base.Initialize(api);

        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    private float GetPerishRate() {
        return container.GetPerishRate() * 0.75f * Core.ConfigServer.GlobalPerishMultiplier; // Slower perish rate
    }

    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) return container.Room?.ExitCount == 0 ? 2.5f : 0.5f; // Faster dry/melt rate
        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        return 0.75f * Core.ConfigServer.GlobalPerishMultiplier; // Slower perish rate
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

    private bool TryPut(IPlayer byPlayer, ItemSlot slot) {
        if (inv[0].Empty || inv[0].Itemstack.Collectible.Equals(slot.Itemstack.Collectible)) {
            int moved = 0;

            if (byPlayer.Entity.Controls.ShiftKey) {
                for (int i = 0; i < inv.Count; i++) {
                    int availableSpace = inv[i].MaxSlotStackSize - inv[i].StackSize;
                    moved += slot.TryPutInto(Api.World, inv[i], availableSpace);

                    if (slot.StackSize == 0) break;
                }
            }
            else { 
                for (int i = 0; i < inv.Count; i++) {
                    if (inv[i].StackSize < inv[i].MaxSlotStackSize) {
                        moved = slot.TryPutInto(Api.World, inv[i], 1);
                        if (moved > 0) break;
                    }
                }
            }

            if (moved > 0) {
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer) {
        for (int i = inv.Count - 1; i >= 0; i--) {
            if (!inv[i].Empty) {
                ItemStack stack;

                if (byPlayer.Entity.Controls.ShiftKey) stack = inv[i].TakeOutWhole();
                else stack = inv[i].TakeOut(1);

                if (stack != null && stack.StackSize > 0 && byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    MarkDirty();

                    return true;
                }
            }
        }

        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        tesselator.TesselateBlock(block, out MeshData blockMesh);
        if (blockMesh == null) return false;

        MeshData contentMesh = GenLiquidyMesh(capi, GetContentStacks(), ShapeReferences.CeilingJarUtil);
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
        DisplayPerishMultiplier(GetPerishRate(), sb);
        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.ByBlockMerged, slotCount);
    }
}
