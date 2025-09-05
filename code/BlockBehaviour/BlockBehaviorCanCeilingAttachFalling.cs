using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace FoodShelves;

public class BlockBehaviorCanCeilingAttachFalling(Block block) : BlockBehavior(block) {
    private bool ignorePlaceTest;
    private AssetLocation[] exceptions;
    public bool fallSideways;
    private float dustIntensity;
    private float fallSidewaysChance = 0.3f;
    private AssetLocation fallSound;
    private float impactDamageMul;
    private Cuboidi[] attachmentAreas;
    private BlockFacing[] attachableFaces;

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);
        attachableFaces = null;
        if (properties["attachableFaces"].Exists) {
            string[] array = properties["attachableFaces"].AsArray<string>();
            attachableFaces = new BlockFacing[array.Length];
            for (int i = 0; i < array.Length; i++) {
                attachableFaces[i] = BlockFacing.FromCode(array[i]);
            }
        }

        Dictionary<string, RotatableCube> dictionary = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
        attachmentAreas = new Cuboidi[6];
        if (dictionary != null) {
            foreach (KeyValuePair<string, RotatableCube> item in dictionary) {
                item.Value.Origin.Set(8.0, 8.0, 8.0);
                BlockFacing blockFacing = BlockFacing.FromFirstLetter(item.Key[0]);
                attachmentAreas[blockFacing.Index] = item.Value.RotatedCopy().ConvertToCuboidi();
            }
        }
        else {
            attachmentAreas[4] = properties["attachmentArea"].AsObject<Cuboidi>();
        }

        ignorePlaceTest = properties["ignorePlaceTest"].AsBool();
        exceptions = properties["exceptions"].AsObject(Array.Empty<AssetLocation>(), block.Code.Domain);
        fallSideways = properties["fallSideways"].AsBool();
        dustIntensity = properties["dustIntensity"].AsFloat();
        fallSidewaysChance = properties["fallSidewaysChance"].AsFloat(0.3f);
        string text = properties["fallSound"].AsString();
        if (text != null) {
            fallSound = AssetLocation.Create(text, block.Code.Domain);
        }

        impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode) {
        handling = EnumHandling.Handled;
        
        BlockPos attachingBlockPos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
        Block attachingBlock = world.BlockAccessor.GetBlock(attachingBlockPos);
        
        if (blockSel.Face == BlockFacing.DOWN) {
            if (attachingBlock.SideSolid[BlockFacing.DOWN.Index]) {
                block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
        }
        else if (blockSel.Face == BlockFacing.UP) {
            if (attachingBlock.SideSolid[BlockFacing.UP.Index]) {
                block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
        }

        return false;
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling) {
        if (!CanBlockStay(world, pos, out bool _) && world.Side != EnumAppSide.Client) {
            EnumHandling handling2 = EnumHandling.PassThrough;
            string failureCode = "";
            TryFalling(world, pos, ref handling2, ref failureCode);
        }
    }

    public bool CanBlockStay(IWorldAccessor world, BlockPos pos, out bool isCeilingAttached) {
        Block attachingBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
        bool canBlockStay = attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, pos, BlockFacing.DOWN);

        isCeilingAttached = canBlockStay;

        if (!canBlockStay) {
            attachingBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
            canBlockStay = attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, pos, BlockFacing.UP);
        }

        return canBlockStay;
    }

    private bool TryFalling(IWorldAccessor world, BlockPos pos, ref EnumHandling handling, ref string failureCode) {
        if (world.Side != EnumAppSide.Server) return false;
        if (!((world as IServerWorldAccessor).Api as ICoreServerAPI).Server.Config.AllowFallingBlocks) return false;

        if (IsReplacableBeneath(world, pos) || (fallSideways && world.Rand.NextDouble() < (double)fallSidewaysChance && IsReplacableBeneathAndSideways(world, pos))) {
            if (world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(pos)) == null) {
                EntityBlockFalling entity = new(block, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
                world.SpawnEntity(entity);
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            handling = EnumHandling.PreventDefault;
            failureCode = "entityintersecting";
            return false;
        }

        handling = EnumHandling.PassThrough;
        return false;
    }

    private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos) {
        for (int i = 0; i < 4; i++) {
            BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
            Block blockOrNull = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y, pos.Z + blockFacing.Normali.Z);
            if (blockOrNull != null && blockOrNull.Replaceable >= 6000) {
                blockOrNull = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y - 1, pos.Z + blockFacing.Normali.Z);
                if (blockOrNull != null && blockOrNull.Replaceable >= 6000) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos) {
        return world.BlockAccessor.GetBlockBelow(pos).Replaceable > 6000;
    }

    public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null) {
        return false;
    }
}