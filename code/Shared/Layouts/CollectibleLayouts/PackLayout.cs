namespace FoodShelves;

public class PackLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        if (td.item > 3) {
            td.offsetX = -0.1875f;
            td.offsetY = 0.14f;
            td.offsetZ = 0.125f;

            td.offsetRotX = 75f;
            td.offsetRotY = 90f;
            return;
        }

        td.offsetX = (td.item % 2) * 0.31f - 0.16f;
        td.offsetY = (td.item / 2) * 0.13f;
        td.offsetZ = -0.3f;
    }
}