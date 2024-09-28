﻿using System.Linq;

namespace FoodShelves; 

public static class Extensions {
    #region JSONExtensions

    public static void EnsureAttributesNotNull(this CollectibleObject obj) => obj.Attributes ??= new JsonObject(new JObject());

    public static T LoadAsset<T>(this ICoreAPI api, string path) => api.Assets.Get(new AssetLocation(path)).ToObject<T>();

    public static void SetTreeAttributeContents(this ItemStack stack, InventoryGeneric inv, string attributeName, int index = 1) {
        TreeAttribute stacksTree = new();

        for (; index < inv.Count; index++) {
            if (inv[index].Itemstack == null) break;
            stacksTree[index + ""] = new ItemstackAttribute(inv[index].Itemstack);
        }

        stack.Attributes[$"{attributeName}"] = stacksTree;
    }

    public static ItemStack[] GetTreeAttributeContents(this ItemStack itemStack, ICoreClientAPI capi, string attributeName, int index = 1) {
        ITreeAttribute tree = itemStack?.Attributes?.GetTreeAttribute($"{attributeName}");
        List<ItemStack> contents = new();

        if (tree != null) {
            for (; index < tree.Count + 1; index++) {
                ItemStack stack = tree.GetItemstack(index + "");
                stack?.ResolveBlockOrItem(capi.World);
                contents.Add(stack);
            }
        }

        return contents.ToArray();
    }

    #endregion

    #region StringExtensions

    public static string FirstCharToUpper(this string input) {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrEmpty(input)) throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
        return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
    }

    #endregion

    #region MeshExtensions

    public static MeshData BlockYRotation(this MeshData obj, BlockEntity BE) {
        Block block = BE.Api.World.BlockAccessor.GetBlock(BE.Pos);
        return obj.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, block.Shape.rotateY * GameMath.DEG2RAD, 0);
    }

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

    public static int GetStackCacheHashCodeFNV(ItemStack[] contentStack) {
        if (contentStack == null) return 0;

        unchecked {
            // FNV-1 hash since any other simpler one ends up colliding, fuck data structures & algorithms btw
            const uint FNV_OFFSET_BASIS = 2166136261;
            const uint FNV_32_PRIME = 16777619;

            uint hash = FNV_OFFSET_BASIS;

            hash = (hash ^ (uint)contentStack.Length.GetHashCode()) * FNV_32_PRIME;

            for (int i = 0; i < contentStack.Length; i++) {
                if (contentStack[i] == null) continue;

                uint collectibleHash = (uint)(contentStack[i].Collectible != null ? contentStack[i].Collectible.Code.GetHashCode() : 0);
                hash = (hash ^ collectibleHash) * FNV_32_PRIME;
            }

            return (int)hash;
        }
    }

    private static char GetFacingFromBlockCode(BlockEntity block) {
        string codePath = block.Block.Code.ToString();
        if (codePath == null) return 'n';

        string[] parts = codePath.Split('-');
        string facingStr = parts.Last().ToLowerInvariant();

        return facingStr switch {
            "north" => 'n',
            "east" => 'e',
            "south" => 's',
            "west" => 'w',
            _ => 'n'
        };
    }

    // currently hardcoded for BarrelRackBig
    public static int[] GetMultiblockIndex(Vec3i offset, BlockEntity block) {
        char facing = GetFacingFromBlockCode(block);

        int transformedX = offset.X;
        int transformedY = offset.Y;
        int transformedZ = offset.Z;

        switch (facing) {
            case 'n':
                break; // No change needed for North
            case 's':
                transformedX -= 1;
                transformedZ += 1;
                break;
            case 'e':
                transformedZ += 1;
                break;
            case 'w':
                transformedX -= 1;
                break;
        }

        return new int[3] { transformedX, transformedY, transformedZ };
    }

    #endregion

    #region GeneralBlockExtensions

    public static T GetBlockEntityExt<T>(this IBlockAccessor blockAccessor, BlockPos pos) where T : BlockEntity {
        if (blockAccessor.GetBlockEntity<T>(pos) is T blockEntity) {
            return blockEntity;
        }

        if (blockAccessor.GetBlock(pos) is BlockMultiblock multiblock) {
            BlockPos multiblockPos = new(pos.X + multiblock.OffsetInv.X, pos.Y + multiblock.OffsetInv.Y, pos.Z + multiblock.OffsetInv.Z, pos.dimension);
            return blockAccessor.GetBlockEntity<T>(multiblockPos);
        }

        return null;
    }

    public static ModelTransform GetTransformation(this CollectibleObject obj, Dictionary<string, ModelTransform> transformations) {
        foreach (KeyValuePair<string, ModelTransform> transformation in transformations) {
            if (WildcardUtil.Match(transformation.Key, obj.Code.ToString())) return transformation.Value;
        }

        return null;
    }

    public static string GetMaterialNameLocalized(this ItemStack itemStack, string[] variantKeys = null, string[] toExclude = null) { // Needs to be revised.
        string material = "";

        if (variantKeys == null) {
            material = itemStack.Collectible.Variant["type"];
        }
        else {
            for (int i = 0; i < variantKeys.Length; i++) {
                if (itemStack.Collectible.Variant.ContainsKey(variantKeys[i])) {
                    material = itemStack.Collectible.Variant[variantKeys[i]];
                    break;
                }
            }
        }

        if (toExclude == null) {
            material = material.Replace("normal", "");
        }
        else {
            for (int i = 0; i < toExclude.Length; i++) {
                material = material.Replace(toExclude[i], "");
            }
        }

        if (material == "") return "";
        return " (" + Lang.Get("material-" + material) + ")";
    }

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

    public static int GetRotationAngle(Block block) {
        string blockPath = block.Code.Path;
        if (blockPath.EndsWith("-north")) return 270;
        if (blockPath.EndsWith("-south")) return 90;
        if (blockPath.EndsWith("-east")) return 0;
        if (blockPath.EndsWith("-west")) return 180;
        return 0;
    }

    public static Cuboidf RotateCuboid90Deg(Cuboidf cuboid, int angle) {
        if (angle == 0) {
            return cuboid;
        }

        float x1 = cuboid.X1;
        float y1 = cuboid.Y1;
        float z1 = cuboid.Z1;
        float x2 = cuboid.X2;
        float y2 = cuboid.Y2;
        float z2 = cuboid.Z2;

        return angle switch {
            90 => new Cuboidf(1 - z2, y1, x1, 1 - z1, y2, x2),
            180 => new Cuboidf(1 - x2, y1, 1 - z2, 1 - x1, y2, 1 - z1),
            270 => new Cuboidf(z1, y1, 1 - x2, z2, y2, 1 - x1),
            _ => throw new ArgumentException("Angle must be 0, 90, 180, or 270 degrees"),
        };
    }

    #endregion
}
