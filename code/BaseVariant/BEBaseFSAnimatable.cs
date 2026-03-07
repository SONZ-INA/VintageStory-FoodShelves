using System.Linq;

namespace FoodShelves;

public abstract class BEBaseFSAnimatable : BEBaseFSContainer {
    protected MeshData? ownMesh;
    protected BlockEntityAnimationUtil? AnimUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

    protected virtual string ReferencedShape {
        get {
            var className = this.GetType().Name.Replace("BE", "");
            var field_name = typeof(ShapeReferences).GetField(className, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return field_name?.GetValue(null) as string
                ?? throw new InvalidOperationException($"No shape reference found for {className}");
        }
    }

    public override void Initialize(ICoreAPI api) {
        BlockEntityBehavior behavior = api.World.ClassRegistry.CreateBlockEntityBehavior(this, "Animatable");
        Behaviors.Add(behavior);

        base.Initialize(api);
    }

    protected abstract void HandleAnimations();

    protected MeshData? GenMesh() {
        string?[] parts = [.. VariantAttributes.Values.Select(attr => attr.ToString())];

        string blockName = this.GetType().Name.Replace("BE", "");

        string key = blockName + "Meshes" + Block.Code.ToShortString();
        Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () => {
            return new Dictionary<string, MeshData>();
        });

        Shape? shape = null;
        if (AnimUtil != null) {
            string skeydict = blockName + "Meshes";
            Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate(Api, skeydict, () => {
                return new Dictionary<string, Shape>();
            });

            string sKey = blockName + "Shape" + '-' + block?.Code.ToShortString() + '-' + string.Join('-', parts);
            if (!shapes.TryGetValue(sKey, out shape)) {
                AssetLocation shapeLocation = new(ReferencedShape);
                shape = Shape.TryGet(capi, shapeLocation);
                shapes[sKey] = shape;
            }
        }

        if (shape == null) return null;

        string meshKey = blockName + "Anim" + '-' + block?.Code.ToShortString() + '-' + string.Join('-', parts);
        if (meshes.TryGetValue(meshKey, out MeshData? mesh)) {
            if (AnimUtil != null && AnimUtil.renderer == null) {
                AnimUtil.InitializeAnimator(key, mesh, shape, new Vec3f(0, block?.GetRotationAngle() ?? 0, 0));
            }

            return mesh;
        }

        if (AnimUtil != null) {
            if (AnimUtil.renderer == null) {
                shape.ApplyVariantTextures(this);
                ITexPositionSource texSource = new ShapeTextureSource(capi, shape, $"FS-{blockName}Animation");
                mesh = AnimUtil.InitializeAnimator(key, shape, texSource, new Vec3f(0, block?.GetRotationAngle() ?? 0, 0));
            }

            return meshes[meshKey] = mesh!;
        }

        return null;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            if (ownMesh == null) {
                ownMesh = GenMesh();
                if (ownMesh == null) return false;
            }

            mesher.AddMeshData(ownMesh.Clone().BlockYRotation(block));
            HandleAnimations();
        }

        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);

        TreeAttributeSerializer.DeserializeFromTree(this, tree);

        HandleAnimations();
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        TreeAttributeSerializer.SerializeToTree(this, tree);
    }
}
