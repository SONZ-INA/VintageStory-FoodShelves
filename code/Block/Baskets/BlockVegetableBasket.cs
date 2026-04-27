namespace FoodShelves;

public class BlockVegetableBasket : BaseFSBasket {
    public static RestrictionData VegetableBasketData { get; set; } = new();
    
    public override int InnerSlotCount => 36;

    private static readonly ExplicitTransform LargeTransformations = new (
        X:  [ .17f,   0f,-.15f,  .1f,-.15f, .17f,-.15f,   0 ],
        Y:  [ .03f, .03f, .03f, .15f, .15f, .17f, .18f, .2f ],
        Z:  [ -.1f,  .1f,-.11f,  .1f,  .1f,-.11f,-.11f,   0 ],

        RX: [   -2,    0,    0,   15,   -3,    0,    0,  -2 ],
        RY: [    7,   -2,   15,  -10,   10,    0,   30,  12 ],
        RZ: [    1,   -1,    0,  -45,   25,  -20,   10,   3 ]
    );

    private static readonly ExplicitTransform MediumTransformations = new (
        X:  [    0,  .3f, .11f, .04f,-.22f,  .3f, -.11f, -.1f, .07f, .13f,-.07f,    0 ],
        Y:  [-.03f,-.03f,-.03f, .14f,    0, .11f,  .02f,  .1f, .38f,  .1f, .12f, .12f ],
        Z:  [-.15f,    0, .11f, .14f, .22f,-.25f, -.03f, -.1f,    0, .15f, .02f,    0 ],

        RX: [   -2,   0,    0,     3,   -3,   45,    16,    2,    0,    2,   -1,    1 ],
        RY: [    4,  -2,   10,    -4,  -95,    0,    20,   -1,   -2,    4,   40,  -90 ],
        RZ: [    1,  -1,    0,  -120,    1,    0,   -90,    1,  180,   -3,   -3,   -2 ]
    );

    private static readonly ExplicitTransform StandardTransformations = new(
        X:  [ .24f, .24f, .24f, .09f, .09f, .09f, -.09f,-.09f,-.09f,-.24f,-.24f,-.24f, .17f, .02f,-.07f, .12f, .17f,-.12f, -.3f,-.38f ],
        Y:  [-.04f,-.04f,-.04f,-.04f,-.04f,-.04f, -.04f,-.04f,-.04f,-.04f,-.04f,-.04f, .23f, .18f,  .1f, .23f, .23f, .16f, .18f, .23f ],
        Z:  [-.15f,    0, .15f,-.17f,    0, .15f, -.15f,    0, .15f,-.15f,    0, .15f, -.3f,  .1f,    0,-.17f, .12f,-.13f, .01f, .16f ],

        RX: [   -2,   0,    0,     3,   -3,    2,     3,    2,    0,    2,   -1,    1,   90,   89,    0,   90,   91,    0,   90,   90 ],
        RY: [    4,  -2,   -5,    -4,    0,    0,     4,   -1,   -2,    4,    0,   -1,    2,   20,   20,    0,    0,    0,   21,    1 ],
        RZ: [    1,  -1,    0,    -1,    1,    0,     0,    1,    0,   -3,   -3,   -2,   -4,  -91,  -40,   90,   78,   45,  -85,  -90 ]
    );

    private static readonly ExplicitTransform LongTransformations = new(
        X:  [ .18f,  .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f,  .18f,  .18f,  .18f,  .18f,  .18f,  .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f, .18f, .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f ],
        Y:  [    0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0, .065f, .065f, .065f, .065f, .065f, .065f,.065f,.065f,.065f,.065f,.065f,.065f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f ],
        Z:  [-.19f, -.12f,-.05f, .02f, .09f, .16f,-.18f,-.11f,-.04f, .03f,  .1f, .17f, -.18f, -.11f, -.04f,  .03f,   .1f,  .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f ],

        RX: [    0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,     0,     0,     0,     0,     0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 ],
        RY: [    4,     5,    4,    2,    3,    4,   -2,   -2,   -2,   -2,   -2,   -2,    -1,    -3,    -2,    -3,    -1,    -2,    4,    3,    4,    3,    2,    4,    4,    3,    5,    4,    3,    5,   -2,   -2,   -1,   -2,   -3,   -2 ],
        RZ: [    1,     2,    1,    0,    1,    2,    1,    3,    1,    2,    1,    0,     1,    -1,     1,     0,     1,     1,    0,    1,    1,    2,    1,    1,    1,    1,    1,    1,    1,    0,    1,    2,    1,    1,    0,    1 ]
    );

    public override ExplicitTransform GetTransformationMatrix(string? path) {
        if (path == null) return new([0], [0], [0], [0], [0], [0]);

        foreach (var group in VegetableBasketData.GroupingCodes!) {
            foreach (var code in group.Value) {
                if (WildcardUtil.Match(code, path)) {
                    return group.Key switch {
                        "large" => LargeTransformations,
                        "medium" => MediumTransformations,
                        "standard" => StandardTransformations,
                        "long" => LongTransformations,
                        _ => new([0], [0], [0], [0], [0], [0])
                    };
                }
            }
        }

        return new([0], [0], [0], [0], [0], [0]);
    }

    public override Action<TransformationData>? GetTransformationModifier() {
        return t => {
            t.scaleX = t.scaleY = t.scaleZ = 0.5f;
            t.offsetY = 0.015f;
        };
    }

    protected override MeshData? GenBasketContents(ItemStack? itemstack, ITextureAtlasAPI targetAtlas) {
        if (itemstack == null) return null;

        ItemStack[] contents = GetContents(api.World, itemstack);

        string itemPath = "";
        if (contents.Length > 0 && contents[0] != null) {
            itemPath = contents[0].Collectible.Code;
        }

        MeshData? contentMesh = GenContentMesh(api as ICoreClientAPI, contents, GetTransformationMatrix(itemPath), Transformations, GetTransformationModifier());

        return contentMesh;
    }

    public override bool CanAddToContents(ItemStack[] contents, ItemStack incoming, out int capacity) {
        // Capacity is dynamic based on transformation matrix
        string itemPath = contents.Length > 0 && contents[0] != null
            ? contents[0].Collectible.Code
            : incoming?.Collectible?.Code;

        ExplicitTransform transform = GetTransformationMatrix(itemPath);
        capacity = Math.Min(InnerSlotCount, transform.Length);

        // Only identical items can be put inside
        if (contents.Length > 0 && incoming?.Collectible?.Code != contents[0]?.Collectible?.Code)
            return false;

        return contents.Length < capacity;
    }
}
