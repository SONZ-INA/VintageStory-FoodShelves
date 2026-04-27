[assembly: ModInfo(name: "Food Shelves", modID: "foodshelves")]

namespace FoodShelves;

public class Core : ModSystem {
    private readonly Dictionary<string, RestrictionData> restrictions = [];
    private readonly Dictionary<string, Dictionary<string, ModelTransform>> transformations = [];

    public override void StartPre(ICoreAPI api) {
        ConfigServer.Initialize(api);

        if (api.ModLoader.IsModEnabled("configlib")) {
            _ = new ConfigLibCompatibility(api);
        }
    }

    public override void Start(ICoreAPI api) {
        base.Start(api);

        FSRegistrations.RegisterBlockBehaviors(api);
        FSRegistrations.RegisterBlockClasses(api);
        FSRegistrations.RegisterBlockEntityClasses(api);
    }

    public override void AssetsLoaded(ICoreAPI api) {
        base.AssetsLoaded(api);

        FSDataLoader.LoadAssets(api, restrictions, transformations);
    }

    public override void AssetsFinalize(ICoreAPI api) {
        base.AssetsFinalize(api);

        FSDataFinalizer.FinalizeAssets(api, restrictions, transformations);
    }
}
