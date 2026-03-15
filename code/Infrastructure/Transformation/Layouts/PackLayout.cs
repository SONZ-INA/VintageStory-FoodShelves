namespace FoodShelves;

public class PackLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        if (td.item > 3) {
            td.offsetX = -0.2f;
            td.offsetY = -0.34f;
            td.offsetZ = 0.785f;

            td.offsetRotX = 75f;
            td.offsetRotY = 90f;
            return;
        }

        td.offsetX = (td.item % 2) * 0.33f - 0.18f;
        td.offsetY = (td.item / 2) * 0.13f;
        td.offsetZ = -0.3f;
    }
}