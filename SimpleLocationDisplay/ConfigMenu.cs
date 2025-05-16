using StardewModdingAPI;

namespace SimpleLocationDisplay
{

    /// <summary>The API which lets other mods add a config UI through Generic Mod Config Menu.</summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
    }

    public static class ConfigMenu
    {
        public static void SetupConfigUI(ModEntry modEntry, IModHelper helper, ModConfig config)
        {
            var configMenuApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenuApi == null)
            {
                modEntry.Monitor.Log("Generic Mod Config Menu not found. Configuration UI will not be available.", LogLevel.Warn);
                return;
            }

            configMenuApi.Register(
                mod: modEntry.ModManifest,
                reset: () =>
                {
                    config.EnableMod = true;
                    config.NotificationDuration = 3500f;
                },
                save: () => helper.WriteConfig(config),
                titleScreenOnly: false
            );

            configMenuApi.AddSectionTitle(
                mod: modEntry.ModManifest,
                text: () => "Simple Location Display",
                tooltip: () => "Settings for the Simple Location Display mod"
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => "Enable Mod",
                tooltip: () => "Show location popups when entering new areas",
                getValue: () => config.EnableMod,
                setValue: value => config.EnableMod = value
            );

            configMenuApi.AddNumberOption(
                mod: modEntry.ModManifest,
                getValue: () => config.NotificationDuration,
                setValue: value => config.NotificationDuration = value,
                name: () => "Notification Duration (ms)",
                tooltip: () => "How long the location popup stays on screen (in milliseconds)",
                min: 1000f,
                max: 10000f,
                interval: 500f
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => "Enable Debug Logging",
                tooltip: () => "Enable debug logging for the mod",
                getValue: () => config.EnableDebugLogging,
                setValue: value => config.EnableDebugLogging = value
            );
        }
    }
}