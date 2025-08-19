namespace FoodShelves;

public class BlockMushroomBasket : BaseFSBasket {
    public override float[,] GetTransformationMatrix(string path = null) {
        float[] x = [ .25f, .36f, .25f, .42f,   .4f, .37f,  .23f, .13f, .15f, .42f, .10f, .43f, .13f, .53f, .63f, .33f, .13f, .53f ];  
        float[] y = [ .01f, .01f, .01f, .01f, -.01f, .06f, -.05f, .04f, .05f, .07f, .03f, .05f, .04f, .01f, .01f, .01f, .01f, .02f ];
        float[] z = [ .25f, .21f, .37f,  .4f,  .55f, .42f,  .13f, .23f, .24f, .21f, .28f, .53f, .23f, .13f, .33f, .03f, .43f, .53f ];

        float[] rX = [   0,    0,    0,    0,   -17,    0,    40,   28,    0,    0,    0,    0,    0,   15,    5,   10,  -13,   -5 ];
        float[] rY = [ -10,  -32,   15,    3,     5,    0,     0,    0,  -10,    0,   45,   60,    0,   21,   34,   34,   54,    2 ];
        float[] rZ = [   0,    0,    0,    0,     1,   30,     0,    0,    0,   30,  -12,   21,    0,    0,   15,    0,    0,   15 ];

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }
}
