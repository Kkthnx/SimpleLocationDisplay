using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SimpleLocationDisplay
{
    public class ModEntry : Mod
    {
        private ModConfig config = new();
        private HUDMessage? lastLocationMessage;
        private string? lastLocationName;

        private static readonly Regex GuidRegex = new(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex MineLevelRegex = new(
            @"^UndergroundMine(\d+)$",
            RegexOptions.Compiled
        );

        private static readonly Regex VolcanoLevelRegex = new(
            @"^VolcanoDungeon(\d+)$",
            RegexOptions.Compiled
        );

        private static readonly Dictionary<string, string> TranslationCache = new();

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ModConfig>() ?? new();
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.ConsoleCommands.Add("debug_location", "Prints current location details.", OnDebugLocationCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            TranslationCache.Clear();
            LogDebug("Translation cache cleared on game launch.");
            ConfigMenu.SetupConfigUI(this, Helper, config);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!config.EnableMod || e.NewLocation == null)
                return;

            string locationName = GetLocationName(e.NewLocation);
            if (locationName == lastLocationName)
                return;

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
            if (location == null)
            {
                LogDebug("Location is null, returning default.");
                return "Unknown Location";
            }

            string translationKey = string.Empty;
            string result = string.Empty;

            // Step 1: Try GetDisplayName
            string? displayName = location.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName) && !displayName.StartsWith("(no translation:"))
            {
                LogDebug($"Using GetDisplayName: {displayName}");
                return displayName;
            }
            LogDebug($"GetDisplayName invalid or null: '{displayName ?? "null"}'");

            // Step 2: Try Name with translation or level handling
            string? nameNullable = location.Name;
            string name = nameNullable ?? string.Empty;
            if (!string.IsNullOrEmpty(name) && name != "null")
            {
                LogDebug($"Using Name: {name}");

                // Handle mine levels
                var mineLevelMatch = MineLevelRegex.Match(name);
                if (mineLevelMatch.Success)
                {
                    string levelNumber = mineLevelMatch.Groups[1].Value;
                    translationKey = $"location.UndergroundMine_Level_{levelNumber}";
                    if (TranslationCache.TryGetValue(translationKey, out string? cachedResult) && cachedResult != null)
                    {
                        LogDebug($"Using cached mine level translation: {cachedResult}");
                        return cachedResult;
                    }

                    string? mineTranslation = Helper.Translation.Get("location.UndergroundMine_Level", new { level = levelNumber });
                    LogDebug($"Raw translation for mine level {levelNumber}: '{mineTranslation ?? "null"}'");
                    if (mineTranslation != null && !mineTranslation.StartsWith("(no translation:") && !mineTranslation.Contains("{level}"))
                    {
                        result = mineTranslation;
                    }
                    else
                    {
                        result = $"Underground Mine Level {levelNumber}";
                    }
                    TranslationCache[translationKey] = result;
                    LogDebug($"Translated mine level {levelNumber}: {result}");
                    return result;
                }

                // Handle volcano dungeon levels
                var volcanoLevelMatch = VolcanoLevelRegex.Match(name);
                if (volcanoLevelMatch.Success)
                {
                    string levelNumber = volcanoLevelMatch.Groups[1].Value;
                    translationKey = $"location.VolcanoDungeon_Level_{levelNumber}";
                    if (TranslationCache.TryGetValue(translationKey, out string? cachedResult) && cachedResult != null)
                    {
                        LogDebug($"Using cached volcano level translation: {cachedResult}");
                        return cachedResult;
                    }

                    string? volcanoTranslation = Helper.Translation.Get("location.VolcanoDungeon_Level", new { level = levelNumber });
                    LogDebug($"Raw translation for volcano level {levelNumber}: '{volcanoTranslation ?? "null"}'");
                    if (volcanoTranslation != null && !volcanoTranslation.StartsWith("(no translation:") && !volcanoTranslation.Contains("{level}"))
                    {
                        result = volcanoTranslation;
                    }
                    else
                    {
                        result = $"Volcano Dungeon Level {levelNumber}";
                    }
                    TranslationCache[translationKey] = result;
                    LogDebug($"Translated volcano level {levelNumber}: {result}");
                    return result;
                }

                // Try standard translation
                translationKey = $"location.{name.Replace(" ", "_").Replace(".", "_")}";
                if (TranslationCache.TryGetValue(translationKey, out string? cachedTranslation) && cachedTranslation != null)
                {
                    LogDebug($"Using cached translation: {cachedTranslation}");
                    return cachedTranslation;
                }

                string? translation = Helper.Translation.Get(translationKey);
                if (translation != null && !translation.StartsWith("(no translation:"))
                {
                    result = translation;
                }
                else
                {
                    result = name;
                }
                TranslationCache[translationKey] = result;
                LogDebug($"Translated name: {result}");
                return result;
            }

            // Step 3: Fallback to sanitized UniqueName
            string? uniqueName = location.NameOrUniqueName;
            if (string.IsNullOrEmpty(uniqueName))
            {
                LogDebug("UniqueName is null or empty, using fallback.");
                return "Unknown Location";
            }

            string sanitizedName = SanitizeRawName(uniqueName);
            translationKey = $"location.{sanitizedName.Replace(" ", "_").Replace(".", "_")}";
            if (TranslationCache.TryGetValue(translationKey, out string? cachedSanitized) && cachedSanitized != null)
            {
                LogDebug($"Using cached sanitized translation: {cachedSanitized}");
                return cachedSanitized;
            }

            string? sanitizedTranslation = Helper.Translation.Get(translationKey);
            if (sanitizedTranslation != null && !sanitizedTranslation.StartsWith("(no translation:"))
            {
                result = sanitizedTranslation;
            }
            else
            {
                result = $"location.{sanitizedName}";
            }
            TranslationCache[translationKey] = result;
            LogDebug($"Translated sanitized name: {result}");
            return result;
        }

        private string SanitizeRawName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                LogDebug("SanitizeRawName: Input name is empty, returning default.");
                return "Unknown Location";
            }

            if (name.Length > 36)
            {
                string potentialGuid = name.Substring(name.Length - 36);
                if (GuidRegex.IsMatch(potentialGuid))
                {
                    string baseName = name.Substring(0, name.Length - 36);
                    LogDebug($"Sanitized '{name}' to '{baseName}'");
                    return baseName.Length < 2 ? "Unknown Location" : baseName;
                }
            }

            LogDebug($"SanitizeRawName: No sanitization needed for '{name}'");
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