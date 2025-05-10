namespace FoodShelves;

public class ConfigServer : IModConfig {
    public const string ConfigServerName = "FoodShelvesServer.json";

    public float GlobalPerishMultiplier { get; set; } = 1f;

    public ConfigServer(ICoreAPI api, ConfigServer previousConfig = null) {
        if (previousConfig == null) return;

        GlobalPerishMultiplier = previousConfig.GlobalPerishMultiplier;
    }
}
