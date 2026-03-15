namespace FoodShelves;

/// <summary>
/// A hierarchical state container that stores translation, rotation, and scale properties. Its BuildMatrix method handles the complex 3D math by chaining transforms in a specific order. <br />
/// Use offsets for item-specific manipulation!
/// </summary>
public class TransformationData(BEBaseFSContainer be) {
    private readonly BEBaseFSContainer be = be;

    public int index;
    public int shelf;
    public int segment;
    public int item;

    public float x, y, z;
    public float offsetX, offsetY, offsetZ;
    public float rotX, rotY, rotZ;
    public float offsetRotX, offsetRotY, offsetRotZ;
    public float scaleX, scaleY, scaleZ;
    public float offsetOriginX, offsetOriginY, offsetOriginZ;

    public void Reset() {
        x = y = z = 0;
        offsetX = offsetY = offsetZ = 0;
        rotX = rotY = rotZ = 0;
        offsetRotX = offsetRotY = offsetRotZ = 0;
        scaleX = scaleY = scaleZ = 1;
        offsetOriginX = offsetOriginY = offsetOriginZ = 0;
    }

    public float[] BuildMatrix() {
        Matrixf mat = new();

        mat.Translate(0.5f, 0, 0.5f);

        // Handle block rotation
        int blockRot = be.Block.GetRotationAngle();
        mat.RotateYDeg(blockRot);

        // Handle segment locations
        mat.Translate(x, y, z);
        mat.RotateYDeg(rotY);

        // Handle item offsets
        mat.Translate(offsetX, offsetY, offsetZ);
        mat.RotateXDeg(offsetRotX);
        mat.RotateYDeg(offsetRotY);
        mat.RotateZDeg(offsetRotZ);
        mat.Translate(offsetOriginX, offsetOriginY, offsetOriginZ);
        mat.Scale(scaleX, scaleY, scaleZ);

        mat.Translate(-0.5f, 0, -0.5f);

        return mat.Values;
    }
}