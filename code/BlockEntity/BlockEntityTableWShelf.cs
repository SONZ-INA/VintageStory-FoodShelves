﻿namespace FoodShelves;

public enum TableWShelfPart {
    Table = 0,
    Shelf = 1
}

public class BlockEntityTableWShelf : BlockEntityDisplay {
    readonly InventoryGeneric inv;
    Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    static readonly int slotCount = 2;
    private readonly InfoDisplayOptions displaySelection = InfoDisplayOptions.ByBlock;

    public BlockEntityTableWShelf() { inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotTableWShelf(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);
        base.Initialize(api);
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer, blockSel);
        }
        else {
            if (slot.ShelvableCheck()) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf) return false;

        for (int i = 0; i < slotCount; i++) {
            if (inv[i].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[i]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf) return false;

        for (int i = slotCount - 1; i >= 0; i--) {
            if (!inv[i].Empty) {
                ItemStack stack = inv[i].TakeOut(1);
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

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[slotCount][];

        for (int index = 0; index < slotCount; index++) {
            float scaleValue = 1f;

            // Using vanilla shelf transformations, the pot is too big so need to adjust it
            ItemSlot slot = inv[index];
            if (slot?.Itemstack?.Collectible?.Code.Path.StartsWith("claypot-") == true) scaleValue = 0.85f;

            tfMatrices[index] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Scale(scaleValue, scaleValue, scaleValue)
                .Translate(- 0.5f, 0.36f, index * 0.5f - 0.75f)
                .Values;
        }

        return tfMatrices;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        DisplayInfo(forPlayer, sb, inv, displaySelection, slotCount);
    }
}
