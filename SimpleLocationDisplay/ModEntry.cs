using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SimpleLocationDisplay
{
    public class ModEntry : Mod
    {
        private ModConfig config = new ModConfig();
        private HUDMessage? lastLocationMessage;
        private string? lastLocationName;

        private static readonly Regex GuidRegex = new Regex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Dictionary<string, string?> TranslationCache = new Dictionary<string, string?>();

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.ConsoleCommands.Add("debug_location", "Prints current location details.", OnDebugLocationCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ConfigMenu.SetupConfigUI(this, Helper, config);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!config.EnableMod || e.NewLocation == null) return;

            string locationName = GetLocationName(e.NewLocation);
            if (locationName == lastLocationName) return;

            if (lastLocationMessage != null && Game1.hudMessages.Contains(lastLocationMessage))
            {
                Game1.hudMessages.Remove(lastLocationMessage);
            }

            lastLocationMessage = HUDMessage.ForCornerTextbox(locationName);
            lastLocationMessage.timeLeft = config.NotificationDuration;
            Game1.hudMessages.Add(lastLocationMessage);
            lastLocationName = locationName;

            LogDebug($"Displayed location: {locationName}");
        }

        private void OnDebugLocationCommand(string command, string[] args)
        {
            if (Game1.currentLocation == null)
            {
                Monitor.Log("No current location available.", LogLevel.Info);
                return;
            }

            string name = Game1.currentLocation.Name ?? "null";
            string uniqueName = Game1.currentLocation.NameOrUniqueName ?? "null";
            string displayName = Game1.currentLocation.GetDisplayName() ?? "null";
            string translatedName = GetLocationName(Game1.currentLocation);

            Monitor.Log($"Location Debug: Name='{name}', UniqueName='{uniqueName}', DisplayName='{displayName}', TranslatedName='{translatedName}'", LogLevel.Info);
        }

        private string GetLocationName(GameLocation location)
        {
            string? displayName = location.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                LogDebug($"Using GetDisplayName: {displayName}");
                return displayName;
            }

            string rawName = location.NameOrUniqueName ?? "Unknown Location";
            string baseName = SanitizeRawName(rawName);
            LogDebug($"GetDisplayName failed, using base name: {baseName}");

            string translationKey = $"location.{baseName.Replace(" ", "_").Replace(".", "_")}";
            if (!TranslationCache.TryGetValue(translationKey, out string? translation))
            {
                translation = Helper.Translation.Get(translationKey);
                if (!string.IsNullOrEmpty(translation) && !translation.StartsWith("(no translation:"))
                {
                    TranslationCache[translationKey] = translation;
                    LogDebug($"Found translation for {baseName}: {translation}");
                    return translation;
                }
                else
                {
                    TranslationCache[translationKey] = null; // Cache "no translation" as null
                }
            }
            else if (translation != null)
            {
                LogDebug($"Using cached translation for {baseName}: {translation}");
                return translation;
            }

            string locationName = $"location.{baseName}";
            LogDebug($"No translation found, using location name: {locationName}");
            return locationName;
        }

        private string SanitizeRawName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown Location";

            if (name.Length > 36)
            {
                string potentialGuid = name.Substring(name.Length - 36);
                if (GuidRegex.IsMatch(potentialGuid))
                {
                    string baseName = name.Substring(0, name.Length - 36);
                    LogDebug($"Sanitized raw name from '{name}' to '{baseName}'");
                    return baseName.Length < 2 ? "Unknown Location" : baseName;
                }
            }

            return name.Length < 2 ? "Unknown Location" : name;
        }

        private void LogDebug(string message)
        {
            if (config.EnableDebugLogging)
            {
                Monitor.Log(message, LogLevel.Debug);
            }
        }
    }
}