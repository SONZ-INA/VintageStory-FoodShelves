namespace FoodShelves;

public class MediumItemTransform : ITransformationException {
    public void Apply(BEBaseFSContainer be, TransformationData td) {
        if (be.inv[td.index].Itemstack?.IsMediumItem() == true) {
            td.x -= 0.05f;
            td.z = 0.025f;

            td.offsetX = 0;
            td.offsetZ = -0.05f;
            
            td.rotY = td.item * 180f + 45f;
        }
    }
}
