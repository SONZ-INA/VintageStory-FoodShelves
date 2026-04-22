namespace FoodShelves;

public class BlockFruitBasket : BaseFSBasket {
    public override int InnerSlotCount => 22;

    public override ExplicitTransform GetTransformationMatrix(string? path = null) {
        return new(
            X:  [ .15f, .05f, .18f,    0, .17f, -.1f,-.11f,-.15f,    0,  .1f, -.2f,-.16f,-.03f, .25f, -.2f,-.06f, .1f,  .2f,-.05f,-.04f,  .05f,-.16f ],
            Y:  [    0,    0,    0,    0,    0,    0,    0,    0, .05f, .11f, .08f,  .1f,  .2f, .13f, .12f, .25f,.15f, .11f, .15f,  .2f,  .32f, .18f ],
            Z:  [ .05f,-.05f, .18f, .15f,-.12f,    0,-.12f, .15f,-.15f, .16f, .02f, .15f, .23f, .18f,-.22f,-.15f, .1f,-.15f, -.2f, .04f,  .13f,  .1f ],

            RX: [   -2,    0,    0,   -3,    0,    8,   -6,   -2,  -20,   30,  -20,    5,  -75,   -8,   10,   85,   0,    8,   15,    8,    90,  -10 ],
            RY: [    4,   -2,  -11,   10,    0,    1,   45,    3,   -2,    4,   45,   45,    2,   20,   55,    2,  50,   15,    0,    0,    22,   10 ],
            RZ: [    1,   -1,    0,    1,    0,    1,   -5,    0,  -10,   17,   20,   20,    3,   16,    7,    6, -20,    8,  -25,  -15,    45,  -10 ]
        );
    }

    public override Action<TransformationData>? GetTransformationModifier() {
        return t => {
            t.scaleX = t.scaleY = t.scaleZ = 0.5f;
            t.offsetY = 0.05f;
        };
    }
}