namespace FoodShelves;

public readonly record struct ExplicitTransform (
    float[] X, float[] Y, float[] Z,
    float[] RX, float[] RY, float[] RZ
) {
    public int Length => X?.Length ?? 0;
}