namespace FoodShelves;

public class CheeseLayout : ICollectibleLayout {
    public void Apply(TransformationData td, ItemStack? stack) {
        td.offsetOriginZ = -0.015f;

        if (stack?.IsMediumItem() == true) {
            td.offsetZ = -0.05f;
            td.offsetOriginX = 0.05f;
            td.offsetRotY = td.item * 180f - 90f;
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
