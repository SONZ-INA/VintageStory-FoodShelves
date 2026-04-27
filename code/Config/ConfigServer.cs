namespace FoodShelves;

public class ConfigServer : IModConfig {
    public const string ConfigServerName = "FoodShelvesServer.json";

    public static ConfigServer Instance { get; set; } = null!;

    public bool GlobalBlockBuffs { get; set; } = true;
    public bool LakeIceToCutIce{ get; set; } = false;
    public float GlobalPerishMultiplier { get; set; } = 1f;
    public float CooledBuff { get; set; } = 0.3f;
    public float IceMeltRate { get; set; } = 1f;

    public ConfigServer(ICoreAPI api, ConfigServer? previousConfig = null) {
        if (previousConfig == null) return;

        GlobalPerishMultiplier = previousConfig.GlobalPerishMultiplier;
        LakeIceToCutIce = previousConfig.LakeIceToCutIce;
        CooledBuff = previousConfig.CooledBuff;
        GlobalBlockBuffs = previousConfig.GlobalBlockBuffs;
        IceMeltRate = previousConfig.IceMeltRate;
    }

    public static void Initialize(ICoreAPI api) {
        if (api.Side != EnumAppSide.Server) return;

        Instance = ModConfig.ReadConfig<ConfigServer>(api, ConfigServerName);

        api.World.Config.SetBool("FoodShelves.GlobalBlockBuffs", Instance.GlobalBlockBuffs);
        api.World.Config.SetBool("FoodShelves.LakeIceToCutIce", Instance.LakeIceToCutIce);
        api.World.Config.SetFloat("FoodShelves.GlobalPerishMultiplier", Instance.GlobalPerishMultiplier);
        api.World.Config.SetFloat("FoodShelves.CooledBuff", Instance.CooledBuff);
        api.World.Config.SetFloat("FoodShelves.IceMeltRate", Instance.IceMeltRate);
    }
}
