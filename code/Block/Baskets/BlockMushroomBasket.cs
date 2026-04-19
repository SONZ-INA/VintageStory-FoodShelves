namespace FoodShelves;

public class BlockMushroomBasket : BaseFSBasket {
    public override int InnerSlotCount => 18;

    public override float[,] GetTransformationMatrix(string? path) {
        float[] x = [ -.1f, .07f, .15f,-.16f,  .25f,-.15f,  .18f,    0,-.02f, -.2f,    0, .13f,-.13f, .05f,    0,  .2f,-.05f, .12f ];  
        float[] y = [    0,    0,    0,    0,  .04f, .06f,  .07f, .04f, .07f, .07f, .08f, .07f, .07f, .12f, .13f, .09f, .13f, .14f ];
        float[] z = [ -.1f, .07f, -.2f, .18f,  .23f,    0, -.05f, .15f,-.16f, -.2f,    0, .13f, .13f,-.05f, .13f, -.2f,-.05f,    0 ];

        float[] rX = [   0,    3,    1,    1,   -40,   -1,    -2,   28,  -10,    0,   -1,    5,    1,  -21,    5,   10,  -13,    5 ];
        float[] rY = [ -10,   10,   45,   30,    35,    0,    15,    1,   10,   20,   -3,    3,    0,    9,   34,   34,   54,    2 ];
        float[] rZ = [   1,   -1,    1,    0,    40,   30,     1,    0,    2,   -2,    5,   -1,    1,    1,   15,    0,    0,  -15 ];

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    public override Action<TransformationData>? GetTransformationModifier() {
        return t => {
            t.offsetY = 0.025f;
        };
    }
}
