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
                api.World.Config.SetBool("FoodShelves.GlobalBlockBuffs", ConfigServer.GlobalBlockBuffs);
                api.World.Config.SetFloat("FoodShelves.GlobalPerishMultiplier", ConfigServer.GlobalPerishMultiplier);
                api.World.Config.SetFloat("FoodShelves.CooledBuff", ConfigServer.CooledBuff);
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

        // Coded Variants----------
        api.RegisterBlockClass("FoodShelves.BlockFSContainer", typeof(BaseFSContainer));
        // ------------------------

        api.RegisterBlockBehaviorClass("FoodShelves.CeilingAttachable", typeof(BlockBehaviorCeilingAttachable));
        api.RegisterBlockBehaviorClass("FoodShelves.CanCeilingAttachFalling", typeof(BlockBehaviorCanCeilingAttachFalling));

        // Block Classes----------
        api.RegisterBlockClass("FoodShelves.BlockBarrelRack", typeof(BlockBarrelRack));
        api.RegisterBlockClass("FoodShelves.BlockTunRack", typeof(BlockTunRack));

        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(BlockEggBasket));

        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));
        // ------------------------

        // Block Entity Classes----
        api.RegisterBlockEntityClass("FoodShelves.BEBarrelRack", typeof(BEBarrelRack));
        api.RegisterBlockEntityClass("FoodShelves.BETunRack", typeof(BETunRack));

        api.RegisterBlockEntityClass("FoodShelves.BEEggBasket", typeof(BEEggBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitBasket", typeof(BEFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEVegetableBasket", typeof(BEVegetableBasket));

        api.RegisterBlockEntityClass("FoodShelves.BECeilingJar", typeof(BECeilingJar));
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BECoolingCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayBlock", typeof(BEFoodDisplayBlock));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayCase", typeof(BEFoodDisplayCase));

        api.RegisterBlockEntityClass("FoodShelves.BEPumpkinCase", typeof(BEPumpkinCase));
        api.RegisterBlockEntityClass("FoodShelves.BETableWShelf", typeof(BETableWShelf));

        api.RegisterBlockEntityClass("FoodShelves.BEBarShelf", typeof(BEBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEBreadShelf", typeof(BEBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEEggShelf", typeof(BEEggShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEPieShelf", typeof(BEPieShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESeedShelf", typeof(BESeedShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEShortShelf", typeof(BEShortShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESushiShelf", typeof(BESushiShelf));
        // ------------------------
    }

    public override void AssetsLoaded(ICoreAPI api) {
        base.AssetsLoaded(api);

        if (api.Side == EnumAppSide.Server) {
            Dictionary<string, string[]> restrictionGroupsServer = new() {
                ["barrels"] = new[] { "barrelrack", "tunrack" },
                ["baskets"] = new[] { "fruitbasket", "vegetablebasket", "eggbasket" },
                ["general"] = new[] { "fooduniversal", "holderuniversal", "liquidystuff", "coolingonly" },
                ["shelves"] = new[] { "pieshelf", "breadshelf", "barshelf", "sushishelf", "eggshelf", "seedshelf", "glassjarshelf" },
                ["other"] = new[] { "pumpkincase" }
            };

            LoadData(api, restrictionGroupsServer);
        }

        if (api.Side == EnumAppSide.Client) {
            Dictionary<string, string[]> restrictionGroups = new() {
                ["baskets"] = new[] { "vegetablebasket" }
            };

            LoadData(api, restrictionGroups);
        }

        BlockVegetableBasket.VegetableBasketData = restrictions["vegetablebasket"];
    }

    private void LoadData(ICoreAPI api, Dictionary<string, string[]> restrictionGroups) {
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
