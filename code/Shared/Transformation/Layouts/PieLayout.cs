namespace FoodShelves;

public class PieLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        td.offsetOriginX = -0.015f;

        if (stack?.IsMediumItem() == true) {
            td.offsetZ = -0.025f;
            td.offsetOriginZ = -0.05f;
            td.offsetRotY = td.item * 180 + 180;
            return;
        }

        switch (td.item) {
            case 0: break;
            case 1: td.offsetRotY += -90; break;
            case 2: td.offsetRotY += 90; break;
            case 3: td.offsetRotY += 180; break;
        }
    }
}
