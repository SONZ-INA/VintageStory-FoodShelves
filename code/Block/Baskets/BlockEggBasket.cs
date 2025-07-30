
namespace FoodShelves;

public class BlockEggBasket : BaseFSBasket {
    protected override Dictionary<string, ModelTransform> Transformations => []; // No custom transformations

    public override float[,] GetTransformationMatrix(string path = null) {
        float[] x = [ .25f, .36f, .25f, .42f,  .4f, .37f,  .23f, .23f, .45f, .42f ];  
        float[] y = [ .01f, .01f, .01f, .01f, .05f, .09f, -.08f, .04f, .05f, .07f ];
        float[] z = [ .25f, .21f, .37f,  .4f, .45f, .42f,  .13f, .23f, .24f, .21f ];

        float[] rX = [   0,    0,    0,    0,   -3,    0,    52,   28,    0,    0 ];
        float[] rY = [ -10,  -32,   15,    3,   10,    0,     0,    0,  -10,    0 ];
        float[] rZ = [   0,    0,    0,    0,    1,   30,     0,    0,    0,   30 ];

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }
}
