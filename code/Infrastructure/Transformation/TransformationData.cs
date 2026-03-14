namespace FoodShelves;

/// <summary>
/// Class used data keeper, providing helper funcitons to TransformationGenerator class.
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

    public void Reset() {
        x = y = z = 0;
        offsetX = offsetY = offsetZ = 0;
        rotX = rotY = rotZ = 0;
        offsetRotX = offsetRotY = offsetRotZ = 0;
        scaleX = scaleY = scaleZ = 1;
    }

    public float[] BuildMatrix() {
        var (ox, oz) = RotateOffset(offsetX, offsetZ);

        return new Matrixf()
            .Translate(0.5f, 0, 0.5f)
            .Translate(ox, offsetY, oz)
            .RotateXDeg(rotX)
            .RotateYDeg((be.Block?.Shape.rotateY ?? 0) + rotY)
            .RotateZDeg(rotZ)
            .Scale(scaleX, scaleY, scaleZ)
            .Translate(x - 0.5f, y, z - 0.5f)
            .RotateXDeg(offsetRotX)
            .RotateYDeg(offsetRotY)
            .RotateZDeg(offsetRotZ)
            .Values;
    }

    private (float, float) RotateOffset(float x, float z) {
        int rot = be.Block.GetRotationAngle();

        return rot switch {
            0 => (x, z),
            90 => (z, -x),
            180 => (-x, -z),
            270 => (-z, x),
            _ => (x, z)
        };
    }
}