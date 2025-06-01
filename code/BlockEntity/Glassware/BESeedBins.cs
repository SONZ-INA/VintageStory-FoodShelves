using System.Linq;
using Vintagestory.API.Common;
using static System.Formats.Asn1.AsnWriter;

namespace FoodShelves;

public class BESeedBins : BEBaseFSAnimatable {
    protected new BlockSeedBins block;
    private readonly MeshData[] contentMeshes = new MeshData[4];

    protected override string CantPlaceMessage => "foodshelves:Only seeds can be placed in these jars.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegmentGrouped;

    protected override float PerishMultiplier => 0.75f; // AoG Compatibility

    public override int ShelfCount => 4;
    public override int ItemsPerSegment => 6;

    // No TreeSerializable fields - only client should see the animations.
    public bool Section1Open { get; set; }
    public bool Section2Open { get; set; }
    public bool Section3Open { get; set; }
    public bool Section4Open { get; set; }

    public BESeedBins() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockSeedBins;
        base.Initialize(api);
    }

    protected override void InitMesh() {
        base.InitMesh();

        var stacks = GetContentStacks();
        for (int i = 0; i < 4; i++) {
            var stacksForMesh = stacks.Skip(i * ItemsPerSegment).Take(ItemsPerSegment).ToArray();
            contentMeshes[i] = GenLiquidyMesh(capi, stacksForMesh, ShapeReferences.utilSeedBins, 6f, false)?.Translate(0, .04f, 0).BlockYRotation(block);
        }
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded

    #region Animations

    protected override void HandleAnimations() {
        if (animUtil != null) {
            if (Section1Open) ToggleSeedDoor(1, true);
            else ToggleSeedDoor(1, false);

            if (Section2Open) ToggleSeedDoor(2, true);
            else ToggleSeedDoor(2, false);

            if (Section3Open) ToggleSeedDoor(3, true);
            else ToggleSeedDoor(3, false);

            if (Section4Open) ToggleSeedDoor(4, true);
            else ToggleSeedDoor(4, false);
        }
    }

    private void ToggleSeedDoor(int section, bool open) {
        if (section > 4) return;

        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey($"section{section}open") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = $"section{section}open",
                    Code = $"section{section}open",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey($"section{section}open") == true)
                animUtil.StopAnimation($"section{section}open");
        }

        switch (section) {
            case 1: Section1Open = open; break;
            case 2: Section2Open = open; break;
            case 3: Section3Open = open; break;
            case 4: Section4Open = open; break;
        }
    }

    #endregion

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        for (int i = 0; i < 4; i++) {
            if (contentMeshes[i] == null) continue;
            MeshData contentMesh = contentMeshes[i].Clone();

            switch (GetRotationAngle(block)) {
                case 0: contentMesh.Translate(i * 0.4375f, 0, 0); break;
                case 90: contentMesh.Translate(0, 0, -i * 0.4375f); break;
                case 180: contentMesh.Translate(-i * 0.4375f, 0, 0); break;
                case 270: contentMesh.Translate(0, 0, i * 0.4375f); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }
}
