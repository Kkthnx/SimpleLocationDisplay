using StardewModdingAPI;

namespace SimpleLocationDisplay
{
    /// <summary>
    /// Interface for the Generic Mod Config Menu API, matching version 1.14.1.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>
        /// Register a mod whose config can be edited through the UI.
        /// </summary>
        /// <param name="manifest">The mod's manifest.</param>
        /// <param name="reset">Reset the config to default values.</param>
        /// <param name="save">Save the current config.</param>
        /// <param name="titleScreenOnly">Whether the options can only be edited from the title screen.</param>
        void Register(IManifest manifest, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>
        /// Add a section title to the config UI.
        /// </summary>
        /// <param name="mod">The mod's manifest.</param>
        /// <param name="text">The title text.</param>
        /// <param name="tooltip">The tooltip text, if any.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        /// <summary>
        /// Add a number option to the config UI.
        /// </summary>
        /// <param name="mod">The mod's manifest.</param>
        /// <param name="getValue">Get the current value.</param>
        /// <param name="setValue">Set a new value.</param>
        /// <param name="name">Display name of the option.</param>
        /// <param name="tooltip">Tooltip for the option, if any.</param>
        /// <param name="min">Minimum value, if any.</param>
        /// <param name="max">Maximum value, if any.</param>
        /// <param name="interval">Increment interval, if any.</param>
        /// <param name="formatValue">Format the value for display, if any.</param>
        /// <param name="fieldId">Unique field ID, if any.</param>
        void AddNumberOption(
            IManifest mod,
            Func<int> getValue,
            Action<int> setValue,
            Func<string> name,
            Func<string>? tooltip = null,
            int? min = null,
            int? max = null,
            int? interval = null,
            Func<int, string>? formatValue = null,
            string? fieldId = null
        );
    }

    /// <summary>
    /// Handles GenericModConfigMenu integration for the mod.
    /// </summary>
    public class ConfigMenu
    {
        private readonly IGenericModConfigMenuApi? configMenuApi;
        private readonly ModEntry modEntry;
        private readonly ModConfig config;

        public ConfigMenu(ModEntry modEntry, ModConfig config, IGenericModConfigMenuApi? configMenuApi)
        {
            this.modEntry = modEntry;
            this.config = config;
            this.configMenuApi = configMenuApi;
        }

        public void SetupConfigUI()
        {
            if (configMenuApi == null)
                return;

            // Register the mod with reset and save actions
            configMenuApi.Register(
                manifest: modEntry.ModManifest,
                reset: () => config.NotificationDuration = 3000,
                save: () => modEntry.Helper.WriteConfig(config),
                titleScreenOnly: false
            );

            // Add section title
            configMenuApi.AddSectionTitle(
                mod: modEntry.ModManifest,
                text: () => "Simple Location Display",
                tooltip: () => "Settings for the Simple Location Display mod"
            );

            // Add number option for NotificationDuration
            configMenuApi.AddNumberOption(
                mod: modEntry.ModManifest,
                getValue: () => config.NotificationDuration,
                setValue: value => config.NotificationDuration = value,
                name: () => "Notification duration (ms)",
                tooltip: () => "The duration of the HUD message in milliseconds",
                min: 1000,
                max: 10000,
                interval: 500,
                formatValue: null,
                fieldId: null
            );
        }
    }
}