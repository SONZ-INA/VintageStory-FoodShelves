namespace FoodShelves;

public class LargeLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        td.offsetZ = -0.05f;
    }
}
