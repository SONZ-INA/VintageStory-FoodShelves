namespace FoodShelves;

public class BETableWShelf : BEBaseFSContainer {
    private enum TableWShelfPart {
        Table = 1,
        Shelf = 0
    }

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int SlotCount => 2;

    public BETableWShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf) return false;

        for (int i = 0; i < SlotCount; i++) {
            if (inv[i].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[i]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf) return false;

        for (int i = SlotCount - 1; i >= 0; i--) {
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
        float[][] tfMatrices = new float[SlotCount][];

        for (int index = 0; index < SlotCount; index++) {
            float scaleValue = 1f;
            float offset = 0;

            // Using vanilla shelf transformations, the pot is too big so need to adjust it
            ItemSlot slot = inv[index];
            if (slot?.Itemstack?.Collectible?.Code.Path.StartsWith("claypot-") == true) {
                scaleValue = 0.85f;
                offset = 0.03f;
            }

            tfMatrices[index] = new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Scale(scaleValue, scaleValue, scaleValue)
                .Translate(- 0.5f, 0.185f + offset, index * 0.5f - 0.75f)
                .Values;
        }

        return tfMatrices;
    }
}
