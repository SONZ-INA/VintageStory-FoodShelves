using System.Linq;

namespace FoodShelves;

public class BEWallCabinet : BEBaseFSContainer {
    protected new BlockWallCabinet block;

    public override string InventoryClassName => "shelf";
    public override string AttributeTransformCode => "onshelfTransform";

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public override int SlotCount => 4;

    public bool CabinetOpen { get; set; }
    private float perishMultiplierUnBuffed = 0.75f;

    public BEWallCabinet() { inv = new InventoryGeneric(SlotCount, "shelf-0", null, null); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockWallCabinet;
        base.Initialize(api);

        perishMultiplierUnBuffed = globalBlockBuffs ? 0.75f : 1f;
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        // Open/Close cabinet
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (!CabinetOpen) ToggleCabinetDoor(true, byPlayer);
            else ToggleCabinetDoor(false, byPlayer);
            
            MarkDirty(true);
            return true;
        }

        return base.OnInteract(byPlayer, blockSel);
    }

    #region Animation & Meshing

    private MeshData ownMesh;

    BlockEntityAnimationUtil animUtil {
        get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
    }

    #region Animation

    private void HandleAnimations() {
        if (animUtil != null) {
            if (CabinetOpen) ToggleCabinetDoor(true);
            else ToggleCabinetDoor(false);
        }
    }

    private void ToggleCabinetDoor(bool open, IPlayer byPlayer = null) {
        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "cabinetopen",
                    Code = "cabinetopen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetOpen, byPlayer.Entity, byPlayer, true, 16);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == true)
                animUtil.StopAnimation("cabinetopen");

            PerishMultiplier = perishMultiplierUnBuffed;

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetClose, byPlayer.Entity, byPlayer, true, 16, 0.3f);
        }

        CabinetOpen = open;
    }

    #endregion

    #region Meshing

    private MeshData GenMesh(ITesselatorAPI tesselator) {
        string[] parts = VariantAttributes.Values.Select(attr => attr.ToString()).ToArray();

        string key = "wallCabinetMeshes" + Block.Code.ToShortString();
        Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () => {
            return new Dictionary<string, MeshData>();
        });

        Shape shape = null;
        if (animUtil != null) {
            string skeydict = "wallCabinetMeshes";
            Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate(Api, skeydict, () => {
                return new Dictionary<string, Shape>();
            });

            string sKey = "wallCabinetShape" + '-' + block.Code.ToShortString() + '-' + string.Join('-', parts);
            if (!shapes.TryGetValue(sKey, out shape)) {
                AssetLocation shapeLocation = new(ShapeReferences.WallCabinet);
                shape = Shape.TryGet(capi, shapeLocation);
                shapes[sKey] = shape;
            }
        }

        string meshKey = "wallCabinetAnim" + '-' + block.Code.ToShortString() + '-' + string.Join('-', parts);
        if (meshes.TryGetValue(meshKey, out MeshData mesh)) {
            if (animUtil != null && animUtil.renderer == null) {
                animUtil.InitializeAnimator(key, mesh, shape, new Vec3f(0, GetRotationAngle(block), 0));
            }

            return mesh;
        }

        if (animUtil != null) {
            if (animUtil.renderer == null) {
                shape.ApplyVariantTextures(this);

                ITexPositionSource texSource = new ShapeTextureSource(capi, shape, "FS-WallCabinetAnimation");
                mesh = animUtil.InitializeAnimator(key, shape, texSource, new Vec3f(0, GetRotationAngle(block), 0));
            }

            return meshes[meshKey] = mesh;
        }

        return null;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            if (ownMesh == null) {
                ownMesh = GenMesh(tesselator);
                if (ownMesh == null) return false;
            }

            mesher.AddMeshData(ownMesh.Clone().BlockYRotation(block));
            HandleAnimations();
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int index = 0; index < SlotCount; index++) {
            float x = index % 2 == 0 ? 0.275f : 0.725f;
            float y = index >= 2 ? 0.5625f : 0.0625f;
            float z = 0.25f;

            tfMatrices[index] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Translate(x - 0.5f, y, z - 0.5f)
                .Translate(-0.5f, 0, -0.5f)
                .Values;
        }

        return tfMatrices;
    }

    #endregion

    #endregion

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        CabinetOpen = tree.GetBool("cabinetOpen", false);

        HandleAnimations();
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetBool("cabinetOpen", CabinetOpen);
    }
}
