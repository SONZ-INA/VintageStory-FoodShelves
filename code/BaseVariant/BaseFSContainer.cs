﻿namespace FoodShelves;

public class BaseFSContainer : BlockContainer, IContainedMeshSource {
    protected bool globalBlockBuffs = true;

    public const string FSAttributes = "FSAttributes";

    private string heldDescEntry;
    private bool preventPlacing = false;
    private string placingMessage = "";

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand
        globalBlockBuffs = api.World.Config.GetBool("FoodShelves.GlobalBlockBuffs", true);
        preventPlacing = Attributes["preventPlacing"].AsBool(false);
        placingMessage = Attributes["placingMessage"].AsString("");
        heldDescEntry = globalBlockBuffs 
            ? Attributes["helddescentry"].AsString(Code.FirstCodePart())
            : "";

        LoadVariantsCreative(api, this);
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) {
        return true;
    }

    public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type) {
        return 0; // To prevent the block reducing the cellar rating
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is IFoodShelvesContainer fscontainer) return fscontainer.OnInteract(byPlayer, blockSel);
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public bool BaseOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        return base.GetHeldItemName(itemStack) + " " + itemStack.GetMaterialNameLocalized();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        string entry = "foodshelves:helddesc-" + heldDescEntry;
        string desc = Lang.Get(entry);
        if (desc != entry) {
            dsc.AppendLine();
            dsc.AppendLine(desc);
        }
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {
        if (preventPlacing) {
            (api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get(placingMessage));
            failureCode = "__ignore__";
            return false;
        }
        else {
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec) {
            var stacks = bec.GetContentStacks();
            foreach (var stack in stacks) {
                world.SpawnItemEntity(stack, pos);
            }

            bec.Inventory.Clear();
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
        var stack = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec) {
            if (bec.Inventory.Empty) {
                stack.Attributes.RemoveAttribute("contents"); // To prevent stupid BlockContainer empty attributes
            }
        }

        if (world.BlockAccessor.GetBlockEntity(pos) is IFoodShelvesContainer fscontainer) {
            if (fscontainer?.VariantAttributes?.Count != 0) {
                stack.Attributes[FSAttributes] = fscontainer.VariantAttributes;
            }
        }

        return stack;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        return new ItemStack[] { OnPickBlock(world, pos) };
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer) {
        BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
        drops[0].ResolvedItemstack.SetFrom(handbookStack);
        return drops;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        if (api.Side == EnumAppSide.Server) return;
        
        string meshCacheKey = GetMeshCacheKey(itemstack);
        var meshrefs = GetCacheDictionary(capi, meshCacheKey);

        if (!meshrefs.TryGetValue(meshCacheKey, out MultiTextureMeshRef meshRef)) {
            MeshData mesh = GenMesh(itemstack, capi.BlockTextureAtlas, null);
            meshrefs[meshCacheKey] = meshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        renderinfo.ModelRef = meshRef;
    }

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        return GenBlockVariantMesh(api, itemstack);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack) {
        if (itemstack.Attributes[FSAttributes] is not ITreeAttribute tree) return Code;

        List<string> parts = new();
        foreach (var pair in tree) {
            parts.Add($"{pair.Key}-{pair.Value}"); // No support for various domains across mods. (eg. cloth from "game:" and "wool:" domains)
        }

        return $"{Code}-{string.Join("-", parts)}";
    }
}
