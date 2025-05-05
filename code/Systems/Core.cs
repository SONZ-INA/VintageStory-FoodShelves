using static FoodShelves.Patches;

[assembly: ModInfo(name: "Food Shelves", modID: "foodshelves")]

namespace FoodShelves;

public class Core : ModSystem {
    private readonly Dictionary<string, RestrictionData> restrictions = new();
    private readonly Dictionary<string, Dictionary<string, ModelTransform>> transformations = new();
    // private readonly Dictionary<string, string[2]> connections = new();

    public static ConfigServer ConfigServer { get; set; }
    // public static ConfigClient ConfigClient { get; set; }

    public override void StartPre(ICoreAPI api) {
        switch (api.Side) {
            case EnumAppSide.Server:
                ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, ConfigServer.ConfigServerName);
                api.World.Config.SetFloat("FoodShelves.GlobalPerishMultiplier", ConfigServer.GlobalPerishMultiplier);
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

        // Coded Variants-------
        api.RegisterBlockClass("FoodShelves.BlockFSContainer", typeof(BlockFSContainer));
        // ---------------------

        api.RegisterBlockBehaviorClass("FoodShelves.CeilingAttachable", typeof(BlockBehaviorCeilingAttachable));
        api.RegisterBlockBehaviorClass("FoodShelves.CanCeilingAttachFalling", typeof(BlockBehaviorCanCeilingAttachFalling));

        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));

        api.RegisterBlockEntityClass("FoodShelves.BEPieShelf", typeof(BEPieShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEBreadShelf", typeof(BEBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEBarShelf", typeof(BEBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEEggShelf", typeof(BEEggShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESeedShelf", typeof(BESeedShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESushiShelf", typeof(BESushiShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEShortShelf", typeof(BEShortShelf)); // Placeholder

        api.RegisterBlockEntityClass("FoodShelves.BECeilingJar", typeof(BECeilingJar));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayBlock", typeof(BEFoodDisplayBlock));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayCase", typeof(BEFoodDisplayCase));
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BECoolingCabinet));

        api.RegisterBlockEntityClass("FoodShelves.BEPumpkinCase", typeof(BEPumpkinCase));

        api.RegisterBlockClass("FoodShelves.BlockShelfShort", typeof(BlockShelfShort));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityShelfShort", typeof(BlockEntityShelfShort));
        
        //api.RegisterBlockClass("FoodShelves.BlockGlassJarShelf", typeof(BlockGlassJarShelf));
        //api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassJarShelf", typeof(BlockEntityGlassJarShelf));

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

        //api.RegisterBlockClass("FoodShelves.BlockGlassJar", typeof(BlockGlassJar));
        //api.RegisterBlockEntityClass("FoodShelves.BlockEntityGlassJar", typeof(BlockEntityGlassJar));
    }

    public override void AssetsLoaded(ICoreAPI api) {
        base.AssetsLoaded(api);

        Dictionary<string, string[]> restrictionGroups = new() {
            ["barrels"] = new[] { "barrelrack", "barrelrackbig" },
            ["baskets"] = new[] { "fruitbasket", "vegetablebasket", "eggbasket" },
            ["general"] = new[] { "fooduniversal", "holderuniversal", "liquidystuff", "coolingonly" },
            ["shelves"] = new[] { "pieshelf", "breadshelf", "barshelf", "sushishelf", "eggshelf", "seedshelf", "glassjarshelf" },
            [""] = new[] { "pumpkincase" }
        };

        foreach (var (category, names) in restrictionGroups) {
            foreach (var name in names) {
                string restrictionPath = $"foodshelves:config/restrictions/{category}/{name}.json".Replace("//", "/");
                string transformationPath = $"foodshelves:config/transformations/{category}/{name}.json".Replace("//", "/");

                restrictions[name] = api.LoadAsset<RestrictionData>(restrictionPath);

                if (api.Assets.Exists(transformationPath)) {
                    transformations[name] = api.LoadAsset<Dictionary<string, ModelTransform>>(transformationPath);
                }
            }
        }
    }

    public override void AssetsFinalize(ICoreAPI api) {
        base.AssetsFinalize(api);

        foreach (CollectibleObject obj in api.World.Collectibles) {
            foreach (var restriction in restrictions) {
                transformations.TryGetValue(restriction.Key, out var transformation);
                PatchCollectibleWhitelist(obj, restriction, transformation);
            }
        }
    }
}
