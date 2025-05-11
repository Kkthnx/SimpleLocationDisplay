using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace SimpleLocationDisplay
{
    /// <summary>
    /// A mod that displays the current location name as a HUD notification with translation support.
    /// </summary>
    public class ModEntry : Mod
    {
        private readonly ModConfig config = new ModConfig();
        private HUDMessage? lastLocationMessage;
        private string? lastLocationName;
        private readonly Dictionary<string, string> translationCache = new();
        private ConfigMenu? configMenu;

        /// <summary>
        /// Entry point for the mod, initializing event subscriptions and configuration.
        /// </summary>
        /// <param name="helper">SMAPI mod helper for events and utilities.</param>
        public override void Entry(IModHelper helper)
        {
            // Load config, overwriting default if file exists
            var loadedConfig = helper.ReadConfig<ModConfig>();
            if (loadedConfig != null)
            {
                config.NotificationDuration = loadedConfig.NotificationDuration;
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            translationCache.Clear();

            // Initialize GenericModConfigMenu if available
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
            {
                var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (api != null)
                {
                    configMenu = new ConfigMenu(this, config, api);
                    configMenu.SetupConfigUI();
                }
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            try
            {
                if (e.NewLocation == null)
                    return;

                string locationName = e.NewLocation.GetDisplayName() ?? GetTranslatedOrRawName(e.NewLocation.Name);

                if (locationName == Game1.content.LoadString("Strings\\UI:UnknownLocation") || locationName == "Unknown Location")
                    LogUnknownLocationDetails(e);

                if (lastLocationName == locationName)
                    return;

                if (lastLocationMessage != null && Game1.hudMessages.Contains(lastLocationMessage))
                    Game1.hudMessages.Remove(lastLocationMessage);

                lastLocationMessage = HUDMessage.ForCornerTextbox(locationName);
                lastLocationMessage.timeLeft = config.NotificationDuration;
                Game1.addHUDMessage(lastLocationMessage);
                lastLocationName = locationName;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnWarped: {ex.Message}", LogLevel.Error);
            }
        }

        private string GetTranslatedOrRawName(string? rawName)
        {
            const string unknownLocation = "Unknown Location";
            if (string.IsNullOrEmpty(rawName) || rawName.Length < 4)
                return Game1.content.LoadString("Strings\\UI:UnknownLocation") ?? unknownLocation;

            string language = Game1.content.GetCurrentLanguage().ToString();
            string cacheKey = $"{language}.{rawName}";

            if (translationCache.TryGetValue(cacheKey, out string? translation) && !string.IsNullOrEmpty(translation))
                return translation;

            // Sanitize name and check for translation
            string sanitizedName = rawName.Replace(" ", "_").Replace(".", "_");
            translation = Helper.Translation.Get($"location.{sanitizedName}") ?? rawName;
            translationCache[cacheKey] = translation;

            return translation;
        }

        private void LogUnknownLocationDetails(WarpedEventArgs e)
        {
            Monitor.Log("Unknown location detected, gathering debug info:", LogLevel.Warn);
            Monitor.Log($"NewLocation is {(e.NewLocation == null ? "null" : "non-null")}", LogLevel.Warn);
            Monitor.Log($"Current Location: {Game1.currentLocation?.Name ?? "None"}", LogLevel.Warn);
            Monitor.Log($"Map Name: {Game1.currentLocation?.Map?.Id ?? "None"}", LogLevel.Warn);
            Monitor.Log($"Player Position: X={Game1.player?.Position.X:F1}, Y={Game1.player?.Position.Y:F1}", LogLevel.Warn);
            Monitor.Log($"Facing Direction: {Game1.player?.FacingDirection ?? -1}", LogLevel.Warn);
            Monitor.Log($"Game Time: {Game1.timeOfDay}, Day={Game1.dayOfMonth}, Season={Game1.currentSeason}, Year={Game1.year}", LogLevel.Warn);
            Monitor.Log($"Multiplayer: {Game1.IsMultiplayer}, IsClient={Game1.IsClient}, IsServer={Game1.IsServer}", LogLevel.Warn);
            Monitor.Log($"Current Scene: {Game1.currentMinigame?.GetType()?.Name ?? "None"}", LogLevel.Warn);
            Monitor.Log($"Potential Causes: {(e.NewLocation == null ? "Null location (loading screen, transition, or mod issue)" : "Location lacks GetDisplayName/Name (modded location?)")}", LogLevel.Warn);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            Helper.Events.Player.Warped -= OnWarped;
            Helper.Events.GameLoop.GameLaunched -= OnGameLaunched;
            Helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
        }
    }
}