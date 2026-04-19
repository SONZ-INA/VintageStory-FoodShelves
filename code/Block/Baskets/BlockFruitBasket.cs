namespace FoodShelves;

public class BlockFruitBasket : BaseFSBasket {
    public override int InnerSlotCount => 22;

    public override float[,] GetTransformationMatrix(string? path = null) {
        float[] x = [ .15f, .05f, .18f,    0, .17f, -.1f,-.11f,-.15f,    0,  .1f, -.2f,-.16f,-.03f, .25f, -.2f,-.06f, .1f,  .2f,-.05f,-.04f,  .05f,-.16f ];
        float[] y = [    0,    0,    0,    0,    0,    0,    0,    0, .05f, .11f, .08f,  .1f,  .2f, .13f, .12f, .25f,.15f, .11f, .15f,  .2f,  .32f, .18f ];
        float[] z = [ .05f,-.05f, .18f, .15f,-.12f,    0,-.12f, .15f,-.15f, .16f, .02f, .15f, .23f, .18f,-.22f,-.15f, .1f,-.15f, -.2f, .04f,  .13f,  .1f ];

        float[] rX = [  -2,    0,    0,   -3,    0,    8,   -6,   -2,  -20,   30,  -20,    5,  -75,   -8,   10,   85,   0,    8,   15,    8,    90,  -10 ];
        float[] rY = [   4,   -2,  -11,   10,    0,    1,   45,    3,   -2,    4,   45,   45,    2,   20,   55,    2,  50,   15,    0,    0,    22,   10 ];
        float[] rZ = [   1,   -1,    0,    1,    0,    1,   -5,    0,  -10,   17,   20,   20,    3,   16,    7,    6, -20,    8,  -25,  -15,    45,  -10 ];

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    public override Action<TransformationData>? GetTransformationModifier() {
        return t => {
            t.scaleX = t.scaleY = t.scaleZ = 0.5f;
            t.offsetY = 0.05f;
        };
    }

    protected override MeshData? GenBasketContents(ItemStack? itemstack, ITextureAtlasAPI targetAtlas) {
        if (itemstack == null) return null;

        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData? contentMesh = GenContentMesh(api as ICoreClientAPI, contents, GetTransformationMatrix(), Transformations, GetTransformationModifier());

        return contentMesh;
    }
}