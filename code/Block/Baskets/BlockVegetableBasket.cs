namespace FoodShelves;

public class BlockVegetableBasket : BaseFSBasket {
    public static RestrictionData VegetableBasketData { get; set; } = new();
    
    public override int InnerSlotCount => 36;

    public override float[,] GetTransformationMatrix(string? path) {
        float[] x = [0], y = [0], z = [0], rX = [0], rY = [0], rZ = [0];
        if (path == null) return GenTransformationMatrix(x, y, z, rX, rY, rZ);

        foreach (var group in VegetableBasketData.GroupingCodes!) {
            foreach (var code in group.Value) {
                if (WildcardUtil.Match(code, path)) {
                    switch(group.Key) {
                        case "large":
                            x = [ .17f,   0f,-.15f,  .1f,-.15f, .17f,-.15f,   0 ];
                            y = [ .03f, .03f, .03f, .15f, .15f, .17f, .18f, .2f ];
                            z = [ -.1f,  .1f,-.11f,  .1f,  .1f,-.11f,-.11f,   0 ];

                            rX = [  -2,    0,    0,   15,   -3,    0,    0,  -2 ];
                            rY = [   7,   -2,   15,  -10,   10,    0,   30,  12 ];
                            rZ = [   1,   -1,    0,  -45,   25,  -20,   10,   3 ];
                            return GenTransformationMatrix(x, y, z, rX, rY, rZ);

                        case "medium":
                            x = [    0,  .3f, .11f, .04f,-.22f,  .3f, -.11f, -.1f, .07f, .13f,-.07f,    0 ];
                            y = [-.03f,-.03f,-.03f, .14f,    0, .11f,  .02f,  .1f, .38f,  .1f, .12f, .12f ];
                            z = [-.15f,    0, .11f, .14f, .22f,-.25f, -.03f, -.1f,    0, .15f, .02f,    0 ];

                            rX = [  -2,   0,    0,     3,   -3,   45,    16,    2,    0,    2,   -1,    1 ];
                            rY = [   4,  -2,   10,    -4,  -95,    0,    20,   -1,   -2,    4,   40,  -90 ];
                            rZ = [   1,  -1,    0,  -120,    1,    0,   -90,    1,  180,   -3,   -3,   -2 ];
                            return GenTransformationMatrix(x, y, z, rX, rY, rZ);

                        // TODO
                        case "standard":
                            x = [    0,  .3f, .11f, .04f,-.22f,  .3f, -.11f, -.1f, .07f, .13f,-.07f,    0 ];
                            y = [-.03f,-.03f,-.03f, .14f,    0, .11f,  .02f,  .1f, .38f,  .1f, .12f, .12f ];
                            z = [-.15f,    0, .11f, .14f, .22f,-.25f, -.03f, -.1f,    0, .15f, .02f,    0 ];

                            rX = [  -2,   0,    0,     3,   -3,   45,    16,    2,    0,    2,   -1,    1 ];
                            rY = [   4,  -2,   10,    -4,  -95,    0,    20,   -1,   -2,    4,   40,  -90 ];
                            rZ = [   1,  -1,    0,  -120,    1,    0,   -90,    1,  180,   -3,   -3,   -2 ];
                            return GenTransformationMatrix(x, y, z, rX, rY, rZ);

                        case "long":
                            x = [ .18f,  .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f,  .18f,  .18f,  .18f,  .18f,  .18f,  .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f, .18f, .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f ];
                            y = [    0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0, .065f, .065f, .065f, .065f, .065f, .065f,.065f,.065f,.065f,.065f,.065f,.065f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f ];
                            z = [-.19f, -.12f,-.05f, .02f, .09f, .16f,-.18f,-.11f,-.04f, .03f,  .1f, .17f, -.18f, -.11f, -.04f,  .03f,   .1f,  .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f ];

                            rX = [   0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,     0,     0,     0,     0,     0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 ];
                            rY = [   4,     5,    4,    2,    3,    4,   -2,   -2,   -2,   -2,   -2,   -2,    -1,    -3,    -2,    -3,    -1,    -2,    4,    3,    4,    3,    2,    4,    4,    3,    5,    4,    3,    5,   -2,   -2,   -1,   -2,   -3,   -2 ];
                            rZ = [   1,     2,    1,    0,    1,    2,    1,    3,    1,    2,    1,    0,     1,    -1,     1,     0,     1,     1,    0,    1,    1,    2,    1,    1,    1,    1,    1,    1,    1,    0,    1,    2,    1,    1,    0,    1 ];
                            return GenTransformationMatrix(x, y, z, rX, rY, rZ);
                    }
                }
            }
        }

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    protected override MeshData? GenBasketContents(ItemStack? itemstack, ITextureAtlasAPI targetAtlas) {
        if (itemstack == null) return null;

        ItemStack[] contents = GetContents(api.World, itemstack);

        string itemPath = "";
        if (contents.Length > 0 && contents[0] != null) {
            itemPath = contents[0].Collectible.Code;
        }

        MeshData? contentMesh = GenContentMesh((api as ICoreClientAPI)!, contents, GetTransformationMatrix(itemPath), 0.5f, Transformations);

        return contentMesh;
    }

    public override bool CanAddToContents(ItemStack[] contents, ItemStack incoming, out int capacity) {
        // Capacity is dynamic based on transformation matrix
        string itemPath = contents.Length > 0 && contents[0] != null
            ? contents[0].Collectible.Code
            : incoming?.Collectible?.Code;

        float[,] tm = GetTransformationMatrix(itemPath);
        capacity = Math.Min(InnerSlotCount, tm.GetLength(1));

        // Only identical items can be put inside
        if (contents.Length > 0 && incoming?.Collectible?.Code != contents[0]?.Collectible?.Code)
            return false;

        return contents.Length < capacity;
    }
}
