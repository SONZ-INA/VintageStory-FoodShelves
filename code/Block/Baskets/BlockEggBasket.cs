namespace FoodShelves;

public class BlockEggBasket : BaseFSBasket {
    protected override Dictionary<string, ModelTransform> Transformations => []; // No custom transformations
    public override int InnerSlotCount => 12;

    public override float[,] GetTransformationMatrix(string? path) {
        float[] x = [.092f, .092f, -.05f, -.086f, .11f, -.02f, -.17f,   -.1f, .02f,  .1f, -.05f, -.1f ];  
        float[] y = [    0,     0,     0,      0, .06f,  .06f,  .08f,   .06f, .12f, .06f,  .08f, .13f ];
        float[] z = [ .08f,  -.1f,  -.1f,  .079f, .12f,  .13f,  .11f, -.025f, .07f, -.1f, -.16f, .11f ];

        float[] rX = [   0,     0,     0,      0,   -3,     0,     0,      0,  -35,    0,    13,    2 ];
        float[] rY = [   3,    -4,   -10,      3,    7,    -3,     0,    -20,  -45,   18,   -22,  -15 ];
        float[] rZ = [   0,     0,     0,      0,    1,     0,   -20,      0,    0,    0,     0,  -12 ];

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    public override Action<TransformationData>? GetTransformationModifier() {
        return t => {
            t.offsetY = 0.0275f;
        };
    }
}
