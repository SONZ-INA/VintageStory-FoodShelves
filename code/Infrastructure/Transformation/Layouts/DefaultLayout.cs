namespace FoodShelves;

internal class DefaultLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        td.offsetX = td.item % 2 == 0 ? -0.155f : 0.155f;
        td.offsetZ = (td.item / 2 == 0 ? -0.155f : 0.155f) - 0.05f;
    }
}
