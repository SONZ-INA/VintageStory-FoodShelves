namespace FoodShelves;

public class BlockFruitBasket : BaseFSBasket {
    public override float[,] GetTransformationMatrix(string path = null) {
        float[] x = { .65f, .3f, .3f,  .3f,  .6f, .35f,  .5f, .65f, .35f, .1f,  .6f, .58f, .3f,   .2f, -.1f,  .1f, .1f, .25f,  .2f, .55f,   .6f, .3f };
        float[] y = {    0,   0,   0, .25f,    0, .35f,  .2f, -.3f,  .3f, .2f,  .4f,  .4f, .4f,   .5f, .57f, .05f, .3f, .52f, .55f, .45f, -.65f, .5f };
        float[] z = { .05f,   0, .4f,  .1f, .45f, .35f, .18f,  .7f, .55f, .1f, .02f,  .3f, .7f, -.15f, .15f, -.2f, .9f, .05f,  .6f, .35f,  -.2f, .6f };

        float[] rX = {  -2,   0,   0,   -3,   -3,   28,   16,   -2,   20,  30,  -20,    5, -75,    -8,   10,   85,   0,    8,   15,   -8,    90, -10 };
        float[] rY = {   4,  -2,  15,   -4,   10,   12,   30,    3,   -2,   4,   -5,   -2,   2,    20,   55,    2,  50,   15,    0,    0,    22,  10 };
        float[] rZ = {   1,  -1,   0,   45,    1,   41,    5,   70,   10,  17,   -2,  -20,   3,    16,    7,    6, -20,    8,  -25,   15,    45, -10 };

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    protected override MeshData GenBasketContents(ItemStack itemstack, ITextureAtlasAPI targetAtlas) {
        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData contentMesh = GenContentMesh(api as ICoreClientAPI, targetAtlas, contents, GetTransformationMatrix(), 0.5f, Transformations);

        return contentMesh;
    }
}