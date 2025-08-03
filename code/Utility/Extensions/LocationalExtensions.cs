namespace FoodShelves;

public static class LocationalExtensions {
    /// <summary>
    /// Finds and returns the transformation for the given collectible object from the provided dictionary.<br/>
    /// Used to apply item-specific position/rotation/scale adjustments to avoid clipping or misalignment.
    /// </summary>
    public static ModelTransform GetTransformation(this CollectibleObject obj, Dictionary<string, ModelTransform> transformations) {
        foreach (KeyValuePair<string, ModelTransform> transformation in transformations) {
            if (WildcardUtil.Match(transformation.Key, obj.Code.ToString())) return transformation.Value;
        }

        return null;
    }

    /// <summary>
    /// Applies the given <see cref="ModelTransform"/> to the matrix, modifying its translation, rotation, and scale.
    /// </summary>
    public static void ApplyModelTransformToMatrixF(this Matrixf mat, ModelTransform transformation) {
        if (transformation == null) return;

        mat.Translate(0.5f, 0, 0.5f);

        if (!transformation.Translation.Equals(new FastVec3f(0, 0, 0))) {
            mat.Translate(transformation.Translation.X, transformation.Translation.Y, transformation.Translation.Z);
        }

        if (!transformation.Rotation.Equals(new FastVec3f(0, 0, 0))) {
            mat.RotateXDeg(transformation.Rotation.X);
            mat.RotateYDeg(transformation.Rotation.Y);
            mat.RotateZDeg(transformation.Rotation.Z);
        }

        if (!transformation.ScaleXYZ.Equals(new FastVec3f(1f, 1f, 1f))) {
            mat.Scale(transformation.ScaleXYZ.X, transformation.ScaleXYZ.Y, transformation.ScaleXYZ.Z);
        }

        mat.Translate(-0.5f, 0, -0.5f);
    }

    /// <summary>
    /// Calculates and returns the horizontal rotation angle of a block relative to the player’s position,
    /// rounding it to the nearest 22.5 degrees.<br/> Useful for meshing blocks that can rotate freely (e.g., buckets/baskets).
    /// </summary>
    public static float GetBlockMeshAngle(IPlayer byPlayer, BlockSelection blockSel, bool val) {
        if (val) {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float deg22dot5rad = GameMath.PIHALF / 4;
            float roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
            return roundRad;
        }

        return 0;
    }

    /// <summary>
    /// Generates a compact transformation matrix from individual arrays of translation and rotation values.<br/>
    /// Used to simplify method parameters by grouping position (x, y, z) and rotation (rX, rY, rZ) data
    /// into a single 2D array format.
    /// </summary>
    public static float[,] GenTransformationMatrix(float[] x, float[] y, float[] z, float[] rX, float[] rY, float[] rZ) {
        float[,] transformationMatrix = new float[6, x.Length];

        for (int i = 0; i < x.Length; i++) {
            transformationMatrix[0, i] = x[i];
            transformationMatrix[1, i] = y[i];
            transformationMatrix[2, i] = z[i];
            transformationMatrix[3, i] = rX[i];
            transformationMatrix[4, i] = rY[i];
            transformationMatrix[5, i] = rZ[i];
        }

        return transformationMatrix;
    }

    /// <summary>
    /// Returns the rotation angle in degrees for the block based on its variant suffix (e.g., "-north", "-south").<br/>
    /// For this method to work properly, it *must* align with the rotation defined in the block's type.
    /// </summary>
    public static int GetRotationAngle(this Block block) {
        string blockPath = block.Code.Path;

        return blockPath switch {
            var path when path.EndsWith("-north") => 0,
            var path when path.EndsWith("-south") => 180,
            var path when path.EndsWith("-east") => 270,
            var path when path.EndsWith("-west") => 90,
            _ => 0
        };
    }
}
