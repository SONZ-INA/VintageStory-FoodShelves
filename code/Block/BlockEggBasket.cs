﻿namespace FoodShelves;

public class BlockEggBasket : BlockContainer {
    WorldInteraction[] interactions;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand

        interactions = ObjectCacheUtil.GetOrCreate(api, "eggBasketBlockInteractions", () => {
            List<ItemStack> eggStackList = new();

            foreach(Item item in api.World.Items) {
                if (item.Code == null) continue;

                if (WildcardUtil.Match(EggBasketData.CollectibleCodes, item.Code)) {
                    eggStackList.Add(new ItemStack(item));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = eggStackList.ToArray()
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-remove",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));      
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        // Prevent duplicating of items inside
        if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) {
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityEggBasket frbasket) {
                ItemStack[] contents = frbasket.GetContentStacks();
                ItemStack emptyFruitBasket = new(this);
                world.SpawnItemEntity(emptyFruitBasket, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                for (int i = 0; i < contents.Length; i++) {
                    world.SpawnItemEntity(contents[i], pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string variantName = itemStack.GetMaterialNameLocalized();
        return base.GetHeldItemName(itemStack) + " " + variantName;
    }

    // Rotation logic
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack) {
        bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        BlockEntityEggBasket block = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityEggBasket;
        block.MeshAngle = GetBlockMeshAngle(byPlayer, blockSel, val);

        return val;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityEggBasket frbasket) 
                return frbasket.OnInteract(byPlayer);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (inSlot.Itemstack == null) {
            dsc.AppendLine(Lang.Get("foodshelves:Empty."));
            return;
        }

        ItemStack[] contents = GetContents(world, inSlot.Itemstack);
        PerishableInfoAverageAndSoonest(contents.ToDummySlots(), dsc, world);
    }

    // Mesh rendering for items when inside inventory
    private string MeshRefsCacheKey => this.Code.ToShortString() + "meshRefs";

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        Dictionary<int, MultiTextureMeshRef> meshrefs;

        if (capi.ObjectCache.TryGetValue(MeshRefsCacheKey, out object obj)) {
            meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
        }
        else {
            capi.ObjectCache[MeshRefsCacheKey] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
        }

        ItemStack[] contents = GetContents(api.World, itemstack);
        int hashcode = GetStackCacheHashCodeFNV(contents);

        if (!meshrefs.TryGetValue(hashcode, out MultiTextureMeshRef meshRef)) {
            float[,] transformationMatrix = GetTransformationMatrix();

            capi.Tesselator.TesselateBlock(this, out MeshData basketMesh);
            MeshData contentMesh = GenContentMesh(capi, contents, transformationMatrix, 1f, FruitBasketTransformations);
            if (contentMesh != null) basketMesh.AddMeshData(contentMesh);

            if (basketMesh != null) { 
                meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(basketMesh);
            }
        }

        renderinfo.ModelRef = meshRef;
    }

    public static float[,] GetTransformationMatrix() {
        float[] x = { .25f, .36f, .25f, .42f,  .4f, .37f,  .23f, .23f, .45f, .42f };  
        float[] y = {    0,    0,    0,    0, .05f, .09f, -.08f, .04f, .05f, .07f };
        float[] z = { .25f, .21f, .37f,  .4f, .45f, .42f,  .13f, .23f, .24f, .21f };

        float[] rX = {   0,    0,    0,    0,   -3,    0,    52,   28,    0,    0 };
        float[] rY = { -10,  -32,   15,    3,   10,    0,     0,    0,  -10,    0 };
        float[] rZ = {   0,    0,    0,    0,    1,   30,     0,    0,    0,   30 };

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }
}
