using static FoodShelves.Patches;

[assembly: ModInfo(name: "Food Shelves", modID: "foodshelves")]

namespace FoodShelves;

public class Core : ModSystem {
    public static ConfigServer ConfigServer { get; set; }
    // public static ConfigClient ConfigClient { get; set; }

    public override void StartPre(ICoreAPI api) {
        switch (api.Side) {
            case EnumAppSide.Server:
                ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, ConfigServer.ConfigServerName);
                api.World.Config.SetBool("FoodShelves.EnableVariants", ConfigServer.EnableVariants);

                bool ExpandedFoodsVariants = api.ModLoader.IsModEnabled("expandedfoods") && ConfigServer.EnableVariants;
                bool WildcraftFruitsNutsVariants = api.ModLoader.IsModEnabled("wildcraftfruit") && ConfigServer.EnableVariants;
                bool LongTermFoodVariants = api.ModLoader.IsModEnabled("long-term_food") && ConfigServer.EnableVariants; // Can't bother to change variable names and shit.

                api.World.Config.SetBool("FoodShelves.ExpandedFoodsVariants", ExpandedFoodsVariants);
                api.World.Config.SetBool("FoodShelves.WildcraftFruitsNutsVariants", WildcraftFruitsNutsVariants);
                api.World.Config.SetBool("FoodShelves.EForWFNVariants", ExpandedFoodsVariants || WildcraftFruitsNutsVariants || LongTermFoodVariants);
                break;
            //case EnumAppSide.Client:
            //    ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, ConfigClient.ConfigClientName);
            //    break;
        }

        if (api.ModLoader.IsModEnabled("configlib")) {
            _ = new ConfigLibCompatibility(api);
        }
    }

    public override void Start(ICoreAPI api) {
        base.Start(api);

        api.RegisterBlockBehaviorClass("FoodShelves.CeilingAttachable", typeof(BlockBehaviorCeilingAttachable));
        api.RegisterBlockBehaviorClass("FoodShelves.CanCeilingAttachFalling", typeof(BlockBehaviorCanCeilingAttachFalling));

        api.RegisterBlockClass("FoodShelves.BlockShelfShort", typeof(BlockShelfShort));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityShelfShort", typeof(BlockEntityShelfShort));
        api.RegisterBlockClass("FoodShelves.BlockPieShelf", typeof(BlockPieShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityPieShelf", typeof(BlockEntityPieShelf));
        api.RegisterBlockClass("FoodShelves.BlockBreadShelf", typeof(BlockBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBreadShelf", typeof(BlockEntityBreadShelf));
        api.RegisterBlockClass("FoodShelves.BlockBarShelf", typeof(BlockBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarShelf", typeof(BlockEntityBarShelf));
        api.RegisterBlockClass("FoodShelves.BlockSushiShelf", typeof(BlockSushiShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntitySushiShelf", typeof(BlockEntitySushiShelf));
        api.RegisterBlockClass("FoodShelves.BlockEggShelf", typeof(BlockEggShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityEggShelf", typeof(BlockEntityEggShelf));
        api.RegisterBlockClass("FoodShelves.BlockSeedShelf", typeof(BlockSeedShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntitySeedShelf", typeof(BlockEntitySeedShelf));
        api.RegisterBlockClass("FoodShelves.BlockGlassJarShelf", typeof(BlockGlassJarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassJarShelf", typeof(BlockEntityGlassJarShelf));

        api.RegisterBlockClass("FoodShelves.BlockTableWShelf", typeof(BlockTableWShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityTableWShelf", typeof(BlockEntityTableWShelf));

        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityFruitBasket", typeof(BlockEntityFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityVegetableBasket", typeof(BlockEntityVegetableBasket));
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(BlockEggBasket));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityEggBasket", typeof(BlockEntityEggBasket));

        api.RegisterBlockClass("FoodShelves.BlockBarrelRack", typeof(BlockBarrelRack));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarrelRack", typeof(BlockEntityBarrelRack));
        api.RegisterBlockClass("FoodShelves.BlockBarrelRackBig", typeof(BlockBarrelRackBig));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarrelRackBig", typeof(BlockEntityBarrelRackBig));
        //api.RegisterBlockClass("FoodShelves.BlockFirkinRack", typeof(BlockFirkinRack));
        //api.RegisterBlockEntityClass("FoodShelves.BlockEntityFirkinRack", typeof(BlockEntityFirkinRack));

        api.RegisterBlockClass("FoodShelves.BlockHorizontalBarrelBig", typeof(BlockHorizontalBarrelBig));

        api.RegisterBlockClass("FoodShelves.BlockPumpkinCase", typeof(BlockPumpkinCase));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityPumpkinCase", typeof(BlockEntityPumpkinCase));

        api.RegisterBlockClass("FoodShelves.BlockGlassFood", typeof(BlockGlassFood));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassFood", typeof(BlockEntityGlassFood));
        api.RegisterBlockClass("FoodShelves.BlockGlassFoodCase", typeof(BlockGlassFoodCase));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassFoodCase", typeof(BlockEntityGlassFoodCase));
        //api.RegisterBlockClass("FoodShelves.BlockGlassJar", typeof(BlockGlassJar));
        //api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassJar", typeof(BlockEntityGlassJar));
        api.RegisterBlockClass("FoodShelves.BlockCeilingJar", typeof(BlockCeilingJar));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityCeilingJar", typeof(BlockEntityCeilingJar));

        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityCoolingCabinet", typeof(BlockEntityCoolingCabinet));
    }

    public override void AssetsLoaded(ICoreAPI api) {
        base.AssetsLoaded(api);

        FoodUniversalData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/general/fooduniversal.json");
        FoodUniversalTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/general/fooduniversal.json");
        HolderUniversalData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/general/holderuniversal.json");
        HolderUniversalTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/general/holderuniversal.json");
        LiquidyStuffData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/general/liquidystuff.json");
        CoolingOnlyData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/general/coolingonly.json");

        PieShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/pieshelf.json");
        PieShelfTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/shelves/pieshelf.json");
        BreadShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/breadshelf.json");
        BreadShelfTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/shelves/breadshelf.json");
        BarShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/barshelf.json");
        BarShelfTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/shelves/barshelf.json");
        SushiShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/sushishelf.json");
        EggShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/eggshelf.json");
        SeedShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/seedshelf.json");
        GlassJarShelfData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/shelves/glassjarshelf.json");

        FruitBasketData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/baskets/fruitbasket.json");
        FruitBasketTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/baskets/fruitbasket.json");
        VegetableBasketData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/baskets/vegetablebasket.json");
        VegetableBasketTransformations = api.LoadAsset<Dictionary<string, ModelTransform>>("foodshelves:config/transformations/baskets/vegetablebasket.json");
        EggBasketData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/baskets/eggbasket.json");

        BarrelRackData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/barrels/barrelrack.json");
        BarrelRackBigData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/barrels/barrelrackbig.json");
        //FirkinRackData = api.LoadAsset<RestrictionData.FirkinRackData>("foodshelves:config/restrictions/barrels/firkinrack.json");

        PumpkinCaseData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/pumpkincase.json");
    }

    public override void AssetsFinalize(ICoreAPI api) {
        base.AssetsFinalize(api);

        foreach (CollectibleObject obj in api.World.Collectibles) {
            PatchFoodUniversal(obj, FoodUniversalData);
            PatchHolderUniversal(obj, HolderUniversalData);
            PatchLiquidyStuff(obj, LiquidyStuffData);
            PatchCoolingOnly(obj, CoolingOnlyData);

            PatchPieShelf(obj, PieShelfData);
            PatchBreadShelf(obj, BreadShelfData);
            PatchBarShelf(obj, BarShelfData);
            PatchSushiShelf(obj, SushiShelfData);
            PatchEggShelf(obj, EggShelfData);
            PatchSeedShelf(obj, SeedShelfData);
            PatchGlassJarShelf(obj, GlassJarShelfData);

            PatchFruitBasket(obj, FruitBasketData);
            PatchVegetableBasket(obj, VegetableBasketData);
            PatchEggBasket(obj, EggBasketData);

            PatchBarrelRack(obj, BarrelRackData);
            PatchBarrelRackBig(obj, BarrelRackBigData);
            //PatchFirkinRack(obj, FirkinRackData);

            PatchPumpkinCase(obj, PumpkinCaseData);
        }
    }
}
