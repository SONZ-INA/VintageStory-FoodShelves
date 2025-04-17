﻿using ConfigLib;
using ImGuiNET;

namespace FoodShelves;

// Totally did NOT steal this from Dana
public class ConfigLibCompatibility {
    public ConfigLibCompatibility(ICoreAPI api) {
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("foodshelves:foodshelvesserver"), (id, buttons) => EditConfigServer(id, buttons, api));
        // api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("foodshelves:foodshelvesclient"), (id, buttons) => EditConfigClient(id, buttons, api));
    }

    private void EditConfigServer(string id, ControlButtons buttons, ICoreAPI api) {
        if (buttons.Save) ModConfig.WriteConfig(api, ConfigServer.ConfigServerName, Core.ConfigServer);
        if (buttons.Restore) Core.ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, ConfigServer.ConfigServerName);
        if (buttons.Defaults) Core.ConfigServer = new(api);

        BuildSettingsServer(Core.ConfigServer, id);
    }

    //private void EditConfigClient(string id, ControlButtons buttons, ICoreAPI api) {
    //    if (buttons.Save) ModConfig.WriteConfig(api, ConfigClient.ConfigClientName, Core.ConfigClient);
    //    if (buttons.Restore) Core.ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, ConfigClient.ConfigClientName);
    //    if (buttons.Defaults) Core.ConfigClient = new(api);

    //    BuildSettingsClient(Core.ConfigClient, id);
    //}

    private void BuildSettingsServer(ConfigServer config, string id) {
        if (config == null) return;

        config.EnableVariants = OnCheckBox(id, config.EnableVariants, nameof(config.EnableVariants));
        config.GlobalPerishMultiplier = OnInputFloat(id, config.GlobalPerishMultiplier, nameof(config.GlobalPerishMultiplier), 0);
    }

    private void BuildSettingsClient(ConfigClient config, string id) {
        if (config == null) return;

        config.AlternativeCoolingCabinetKeymap = OnCheckBox(id, config.AlternativeCoolingCabinetKeymap, nameof(config.AlternativeCoolingCabinetKeymap));
    }

    private bool OnCheckBox(string id, bool value, string name) {
        bool newValue = value;
        ImGui.Checkbox(Lang.Get(name) + $"##{name}-{id}", ref newValue);
        return newValue;
    }

    private float OnInputFloat(string id, float value, string name, float minValue) {
        float newValue = value;
        ImGui.InputFloat(Lang.Get(name) + $"##{name}-{id}", ref newValue, step: 0.05f, step_fast: 1.0f);
        return newValue < minValue ? minValue : newValue;
    }
}

