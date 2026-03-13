namespace FoodShelves;

public class StandardItemTransform : ITransformationException {
    public void Apply(BEBaseFSContainer be, TransformationData td) {
        if (be.inv[td.index].Itemstack?.IsStandardItem() == true) {
            td.offsetX = 0;
            td.offsetZ = -0.04f;
            td.rotY = td.item * 90f + 45f;
        }
    }
}
