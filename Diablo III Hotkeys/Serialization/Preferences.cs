using System.Text.Json.Serialization;

namespace DiabloIIIHotkeys.Serialization
{
    public class Preferences
    {
        [JsonPropertyName("toggleProfileMacro")]
        public ProfileMacro ToggleProfileMacro { get; set; }

        [JsonPropertyName("skillKeybindOverrides")]
        public KeybindOverrides SkillKeybindOverrides { get; set; }
    }
}
