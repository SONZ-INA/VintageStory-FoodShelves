namespace FoodShelves;

public class ConfigClient : IModConfig {
    public const string ConfigClientName = "FoodShelvesClient.json";

    public bool AlternativeCoolingCabinetKeymap { get; set; } = false;

    public ConfigClient(ICoreAPI api, ConfigClient previousConfig = null) {
        if (previousConfig == null) return;

        AlternativeCoolingCabinetKeymap = previousConfig.AlternativeCoolingCabinetKeymap;
    }
}
