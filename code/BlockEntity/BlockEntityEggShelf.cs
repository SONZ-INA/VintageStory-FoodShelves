﻿namespace FoodShelves;

public class BlockEntityEggShelf : BlockEntityDisplay {
    private InventoryGeneric inv;
    private Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    private const int shelfCount = 4;
    private const int segmentsPerShelf = 5;
    private int itemsPerSegment = 4;
    private float globalPerishMultiplier = 1f;

    public BlockEntityEggShelf() { inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotEggShelf(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);
        globalPerishMultiplier = api.World.Config.GetFloat("FoodShelves.GlobalPerishMultiplier", 1f);

        base.Initialize(api);

        if (block.Code.SecondCodePart().StartsWith("short")) {
            itemsPerSegment /= 2;

            // Need to save items and transfer it over to new inventory, they disappear otherwise
            List<ItemStack> stack = new();
            foreach (var slot in inv) stack.Add(slot.Itemstack);
            stack.ToArray();

            inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotEggShelf(inv));

            for (int i = 0; i < shelfCount * segmentsPerShelf * itemsPerSegment; i++) {
                if (i >= stack.Count) break;
                inv[i].Itemstack = stack[i];
            }

            Inventory.LateInitialize(Inventory.InventoryID, api);
        }

        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    private float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier;
    }

    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        return 1 * globalPerishMultiplier;
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer, blockSel);
        }
        else {
            if (slot.EggShelfCheck()) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only eggs can be placed on this shelf."));
            return false;
        }
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * itemsPerSegment;

        for (int i = 0; i < itemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * itemsPerSegment;

        for (int i = itemsPerSegment - 1; i >= 0; i--) {
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

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[shelfCount * segmentsPerShelf * itemsPerSegment][];

        for (int shelf = 0; shelf < shelfCount; shelf++) {
            for (int segment = 0; segment < segmentsPerShelf; segment++) {
                for (int item = 0; item < itemsPerSegment; item++) {
                    int index = shelf * (segmentsPerShelf * itemsPerSegment) + segment * itemsPerSegment + item;

                    float x = segment * 0.172f;
                    float y = shelf * 0.25f;
                    float z = item * 0.1875f;

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Translate(x - 0.84375f, y + 0.06f, z - 0.8125f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        DisplayPerishMultiplier(GetPerishRate(), sb);
        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, shelfCount * segmentsPerShelf * itemsPerSegment, segmentsPerShelf, itemsPerSegment);
    }
}
