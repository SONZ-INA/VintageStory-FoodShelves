using static FoodShelves.DataRegistry;
using static FoodShelves.Patches;

[assembly: ModInfo(name: "Food Shelves", modID: "foodshelves")]

namespace FoodShelves;

public class Core : ModSystem {
    private readonly Dictionary<string, RestrictionData> restrictions = [];
    private readonly Dictionary<string, Dictionary<string, ModelTransform>> transformations = [];

    public static ConfigServer? ConfigServer { get; set; }
    // public static ConfigClient ConfigClient { get; set; }

    public override void StartPre(ICoreAPI api) {
        switch (api.Side) {
            case EnumAppSide.Server:
                ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, ConfigServer.ConfigServerName);
                api.World.Config.SetBool("FoodShelves.GlobalBlockBuffs", ConfigServer.GlobalBlockBuffs);
                api.World.Config.SetBool("FoodShelves.LakeIceToCutIce", ConfigServer.LakeIceToCutIce);
                api.World.Config.SetFloat("FoodShelves.GlobalPerishMultiplier", ConfigServer.GlobalPerishMultiplier);
                api.World.Config.SetFloat("FoodShelves.CooledBuff", ConfigServer.CooledBuff);
                api.World.Config.SetFloat("FoodShelves.IceMeltRate", ConfigServer.IceMeltRate);
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

        // Block Classes-----------
        api.RegisterBlockClass("FoodShelves.BlockBarrelRack", typeof(BlockBarrelRack));
        api.RegisterBlockClass("FoodShelves.BlockTunRack", typeof(BlockTunRack));

        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(BlockEggBasket));
        api.RegisterBlockClass("FoodShelves.BlockMushroomBasket", typeof(BlockMushroomBasket));

        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));
        api.RegisterBlockClass("FoodShelves.BlockMeatFreezer", typeof(BlockMeatFreezer));
        api.RegisterBlockClass("FoodShelves.BlockWallCabinet", typeof(BlockWallCabinet));
        api.RegisterBlockClass("FoodShelves.BlockFruitCooler", typeof(BlockFruitCooler));
        //api.RegisterBlockClass("FoodShelves.BlockGlassJar", typeof(BlockGlassJar));

        api.RegisterBlockClass("FoodShelves.BlockCeilingRack", typeof(BlockCeilingRack));

        api.RegisterBlockClass("FoodShelves.BlockDoubleShelf", typeof(BlockDoubleShelf));
        // ------------------------

        // Block Entity Classes----
        api.RegisterBlockEntityClass("FoodShelves.BEBarrelRack", typeof(BEBarrelRack));
        api.RegisterBlockEntityClass("FoodShelves.BETunRack", typeof(BETunRack));

        api.RegisterBlockEntityClass("FoodShelves.BEEggBasket", typeof(BEEggBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitBasket", typeof(BEFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEVegetableBasket", typeof(BEVegetableBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEMushroomBasket", typeof(BEMushroomBasket));

        api.RegisterBlockEntityClass("FoodShelves.BELargeJar", typeof(BELargeJar));
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BECoolingCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayBlock", typeof(BEFoodDisplayBlock));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayCase", typeof(BEFoodDisplayCase));
        api.RegisterBlockEntityClass("FoodShelves.BEMeatFreezer", typeof(BEMeatFreezer));
        api.RegisterBlockEntityClass("FoodShelves.BEWallCabinet", typeof(BEWallCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BESeedBins", typeof(BESeedBins));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitCooler", typeof(BEFruitCooler));
        //api.RegisterBlockEntityClass("FoodShelves.BEGlassJar", typeof(BEGlassJar));

        api.RegisterBlockEntityClass("FoodShelves.BEPumpkinCase", typeof(BEPumpkinCase));
        api.RegisterBlockEntityClass("FoodShelves.BETableWShelf", typeof(BETableWShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEFlourSack", typeof(BEFlourSack));
        api.RegisterBlockEntityClass("FoodShelves.BEBucketHook", typeof(BEBucketHook));
        api.RegisterBlockEntityClass("FoodShelves.BECeilingRack", typeof(BECeilingRack));

        api.RegisterBlockEntityClass("FoodShelves.BEBarShelf", typeof(BEBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEBreadShelf", typeof(BEBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEDoubleShelf", typeof(BEDoubleShelf));
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
            var restrictionGroupsServer = DiscoverRestrictionGroups(api);
            LoadData(api, restrictionGroupsServer, restrictions, transformations);
        }

        if (api.Side == EnumAppSide.Client) {
            Dictionary<string, string[]> restrictionGroups = new() {
                ["baskets"] = ["vegetablebasket"]
            };

            LoadData(api, restrictionGroups, restrictions, transformations);
        }

        BlockVegetableBasket.VegetableBasketData = restrictions["vegetablebasket"];
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
