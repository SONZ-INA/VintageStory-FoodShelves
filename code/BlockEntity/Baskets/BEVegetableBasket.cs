namespace FoodShelves;

public class BEVegetableBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilVegetableBasket;
    protected override string CantPlaceMessage => "foodshelves:Only vegetables can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int SlotCount => 36;

    public BEVegetableBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        float[,] transformationMatrix = block.GetTransformationMatrix(inv[0]?.Itemstack?.Collectible?.Code);
        int offset = transformationMatrix.GetLength(1);

        int moved = 0;

        for (int i = 0; i < offset; i++) {
            if (inv[i].Empty && (inv[0].Empty || slot?.Itemstack?.Collectible?.Code == inv[0]?.Itemstack?.Collectible?.Code)) {
                moved += slot.TryPutInto(Api.World, inv[i]);
                if (!byPlayer.Entity.Controls.CtrlKey) break;
            }
        }

        if (moved > 0) (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        return moved > 0;
    }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = block.GetTransformationMatrix(inv[0]?.Itemstack?.Collectible?.Code);

        float[][] tfMatrices = new float[36][];
        int offset = transformationMatrix.GetLength(1);

        for (int i = 0; i < offset; i++) {
            tfMatrices[i] = new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY + MeshAngle * GameMath.RAD2DEG)
                .RotateXDeg(transformationMatrix[3, i])
                .RotateYDeg(transformationMatrix[4, i])
                .RotateZDeg(transformationMatrix[5, i])
                .Scale(0.5f, 0.5f, 0.5f)
                .Translate(transformationMatrix[0, i] - 0.84375f, transformationMatrix[1, i], transformationMatrix[2, i] - 0.8125f)
                .Values;
        }

        return tfMatrices;
    }
}
