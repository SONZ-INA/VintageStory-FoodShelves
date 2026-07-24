namespace FoodShelves;

public class ConfigClient : IModConfig {
    public const string ConfigClientName = "FoodShelvesClient.json";

    public static ConfigClient Instance { get; set; } = null!;

    public bool ShowBarrelLabel { get; set; } = true;
    public static bool ShowBarrelLabelEnabled => Instance.ShowBarrelLabel;

    public ConfigClient(ICoreAPI api, ConfigClient? previousConfig = null) {
        if (previousConfig == null) return;

        ShowBarrelLabel = previousConfig.ShowBarrelLabel;
    }

    public static void Initialize(ICoreAPI api) {
        if (api.Side != EnumAppSide.Client) return;

        Instance = ModConfig.ReadConfig<ConfigClient>(api, ConfigClientName);
    }
}
