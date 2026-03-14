namespace FoodShelves;

public class CoolingCabinetTransform : ITransformationException {
    public void Apply(BEBaseFSContainer be, TransformationData td) {
        ItemStack? itemStack = be.inv[td.index].Itemstack;
        string itemPath = itemStack?.Collectible?.Code.Path ?? "";

        if (itemStack?.IsLargeItem() == true) {
            td.z += td.item * 0.65f;
            return;
        }

        if (itemStack?.IsMediumItem() == true) {
            td.x += td.item * 1.215f - 0.1075f;
            td.offsetZ += td.item * 0.6125f + 0.125f;
            td.offsetRotY = td.item * 180f + 20f;

            if (itemPath.Contains("pie")) {
                td.offsetRotY -= 90;
                td.x += td.item == 0 ? 0.95f : -0.95f;
                td.z += td.item == 0 ? -0.35f : 0.35f;
            }

            return;
        }

        if (itemStack?.IsSmallItem() == true) {
            td.x += td.segment * 0.121f + (td.item % 4) * 0.19f - 0.285f;
            td.y = td.y * 1.16f + (td.item / 8) * 0.10f + 0.045f;
            td.z += ((td.item / 4) % 2) * 0.4f - 0.225f;
            td.scaleX = td.scaleY = td.scaleZ = 0.8f;
            return;
        }

        // 4-slice cheese or pie
        if (itemPath.Contains("cheese") || itemPath.Contains("pie")) {
            td.x += (td.index % 2 == 0 ? -0.16f : 0.16f) + 0.16f;

            switch (td.item) {
                case 0:
                    td.z += -0.1f;
                    break;
                case 1:
                    td.x += 0.675f;
                    td.z += -0.1f;
                    td.rotY += -2;
                    td.offsetRotY += -90;
                    break;
                case 2:
                    td.z += 1f;
                    td.offsetRotY += 90;
                    break;
                case 3:
                    td.x += 0.68f;
                    td.z += 1.05f;
                    td.rotY += 2;
                    td.offsetRotY += 180;
                    break;
            }
            return;
        }

        string[] collectibleCodes = ["pemmican-pack", "chips-pack", "mushroom-pack", "nutri-pack"];
        if (collectibleCodes.Contains(itemPath)) {
            if (td.item > 3) {
                td.x += -0.2f;
                td.y += -0.34f;
                td.z += 0.785f;

                td.offsetRotX += 75;
                td.offsetRotY += 90;
                return;
            }
            
            td.x += td.segment * 0.0075f + td.item % 2 * 0.33f - 0.18f;
            td.y += td.item / 2 * 0.13f;
            td.z += -0.3f;
            return;
        }

        td.x += (td.index % 2 == 0 ? -0.16f : 0.16f);
        td.z += (td.index / 2) % 2 == 0 ? -0.18f : 0.18f;
    }
}
