namespace FoodShelves;

public class ConfigServer : IModConfig {
    public const string ConfigServerName = "FoodShelvesServer.json";

    public bool GlobalBlockBuffs { get; set; } = true;
    public bool LakeIceToCutIce{ get; set; } = false;
    public float GlobalPerishMultiplier { get; set; } = 1f;
    public float CooledBuff { get; set; } = 0.3f;
    public float IceMeltRate { get; set; } = 1f;

    public ConfigServer(ICoreAPI api, ConfigServer previousConfig = null) {
        if (previousConfig == null) return;

        GlobalPerishMultiplier = previousConfig.GlobalPerishMultiplier;
        LakeIceToCutIce = previousConfig.LakeIceToCutIce;
        CooledBuff = previousConfig.CooledBuff;
        GlobalBlockBuffs = previousConfig.GlobalBlockBuffs;
        IceMeltRate = previousConfig.IceMeltRate;
    }
}
