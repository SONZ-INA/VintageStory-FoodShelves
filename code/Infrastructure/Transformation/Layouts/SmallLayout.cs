namespace FoodShelves;

public class SmallLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        td.offsetX = td.item % 4 * 0.155f - 0.2325f;
        td.offsetY = td.item / 8 * 0.0825f;
        td.offsetZ = td.item / 4 % 2 * 0.365f - 0.24f;

        td.scaleX = td.scaleY = 0.82f;
    }
}

