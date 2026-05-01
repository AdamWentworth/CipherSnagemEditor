using System.Text;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    public GameCubeScriptMacroCatalog BuildScriptMacroCatalog()
        => _scriptMacroCatalog ??= CreateScriptMacroCatalog();

    private GameCubeScriptMacroCatalog CreateScriptMacroCatalog()
    {
        var catalog = new GameCubeScriptMacroCatalog();
        AddStaticScriptMacros(catalog);

        if (TryReadCommonRel(out var data, out var table, out var strings, out _) && data is not null && table is not null)
        {
            AddNamedIdMacros(catalog, "pokemon", "POKEMON", BuildPokemonNameMap(data, table, strings), "POKEMON_NONE");
            AddNamedIdMacros(catalog, "move", "MOVE", BuildMoveNameMap(data, table, strings), "MOVE_NONE");
            AddNamedIdMacros(catalog, "item", "ITEM", BuildItemNameMap(data, table, strings), "ITEM_NONE");
            AddNamedIdMacros(catalog, "room", "ROOM", BuildRoomNameMap(data, table), "ROOM_NONE");
            AddNamedIdMacros(catalog, "battlefield", "BATTLEFIELD", BuildRoomNameMap(data, table), "BATTLEFIELD_NONE");
        }

        AddAbilityMacros(catalog);
        AddTreasureMacros(catalog);
        AddShadowPokemonMacros(catalog);
        return catalog;
    }

    private static void AddStaticScriptMacros(GameCubeScriptMacroCatalog catalog)
    {
        catalog.Add("battleResult", 0, "#RESULT_NONE");
        catalog.Add("battleResult", 1, "#RESULT_LOSE");
        catalog.Add("battleResult", 2, "#RESULT_WIN");
        catalog.Add("battleResult", 3, "#RESULT_TIE");

        catalog.Add("flag", 4, "#FLAG_BERRY_MASTER_VISITED");
        catalog.Add("flag", 5, "#FLAG_STEPS_SINCE_LAST_BERRY_MASTER_VISIT");
        catalog.Add("flag", 964, "#FLAG_STORY");
        catalog.Add("flag", 1124, "#FLAG_DAY_CARE_VISITED");
        catalog.Add("flag", 1191, "#FLAG_HAS_ENCOUNTERED_MIROR_B");
        catalog.Add("flag", 1248, "#FLAG_POKESPOT_ROCK_CURRENT_SNACKS");
        catalog.Add("flag", 1249, "#FLAG_POKESPOT_OASIS_CURRENT_SNACKS");
        catalog.Add("flag", 1250, "#FLAG_POKESPOT_CAVE_CURRENT_SNACKS");
        catalog.Add("flag", 1407, "#FLAG_POKESPOT_ROCK_CURRENT_POKEMON");
        catalog.Add("flag", 1408, "#FLAG_POKESPOT_OASIS_CURRENT_POKEMON");
        catalog.Add("flag", 1409, "#FLAG_POKESPOT_CAVE_CURRENT_POKEMON");
        catalog.Add("flag", 1415, "#FLAG_MIROR_RADAR_KEEP_SIGNAL_ON");
        catalog.Add("flag", 1433, "#FLAG_MT_BATTLE_HIGHEST_CLEARED_ZONE");
        catalog.Add("flag", 1449, "#FLAG_MIROR_B_STEPS_WALKED");
        catalog.Add("flag", 1452, "#FLAG_MIRORB_LOCATION");
        catalog.Add("flag", 1478, "#FLAG_MT_BATTLE_CURRENT_ZONE");
        catalog.Add("flag", 1487, "#FLAG_MT_BATTLE_CHALLENGE_AVAILABLE");
        catalog.Add("flag", 1754, "#FLAG_PYRITE_COLO_WINS");

        catalog.Add("shadowStatus", 0, "#SHADOW_STATUS_NOT_SEEN");
        catalog.Add("shadowStatus", 1, "#SHADOW_STATUS_SEEN_AS_SPECTATOR");
        catalog.Add("shadowStatus", 2, "#SHADOW_STATUS_SEEN_IN_BATTLE");
        catalog.Add("shadowStatus", 3, "#SHADOW_STATUS_CAUGHT");
        catalog.Add("shadowStatus", 4, "#SHADOW_STATUS_PURIFIED");

        catalog.Add("pokespot", 0, "#POKESPOT_ROCK");
        catalog.Add("pokespot", 1, "#POKESPOT_OASIS");
        catalog.Add("pokespot", 2, "#POKESPOT_CAVE");
        catalog.Add("pokespot", 3, "#POKESPOT_ALL");

        catalog.Add("partyMember", 0, "#PARTY_MEMBER_NONE");
        catalog.Add("partyMember", 1, "#PARTY_MEMBER_JOVI");
        catalog.Add("partyMember", 2, "#PARTY_MEMBER_KANDEE");
        catalog.Add("partyMember", 3, "#PARTY_MEMBER_KRANE");

        catalog.Add("buttonInput", 0x001, "#BUTTON_INPUT_D_PAD_LEFT");
        catalog.Add("buttonInput", 0x002, "#BUTTON_INPUT_D_PAD_RIGHT");
        catalog.Add("buttonInput", 0x004, "#BUTTON_INPUT_D_PAD_DOWN");
        catalog.Add("buttonInput", 0x008, "#BUTTON_INPUT_D_PAD_UP");
        catalog.Add("buttonInput", 0x010, "#BUTTON_INPUT_TRIGGER_Z");
        catalog.Add("buttonInput", 0x020, "#BUTTON_INPUT_TRIGGER_R");
        catalog.Add("buttonInput", 0x040, "#BUTTON_INPUT_TRIGGER_L");
        catalog.Add("buttonInput", 0x100, "#BUTTON_INPUT_A");
        catalog.Add("buttonInput", 0x200, "#BUTTON_INPUT_B");
        catalog.Add("buttonInput", 0x400, "#BUTTON_INPUT_X");
        catalog.Add("buttonInput", 0x800, "#BUTTON_INPUT_Y");
        catalog.Add("buttonInput", 0x1000, "#BUTTON_INPUT_START");

        catalog.Add("vectorDimension", 0, "#V_X");
        catalog.Add("vectorDimension", 1, "#V_Y");
        catalog.Add("vectorDimension", 2, "#V_Z");

        catalog.Add("region", 0, "#REGION_JP");
        catalog.Add("region", 1, "#REGION_US");
        catalog.Add("region", 2, "#REGION_PAL");
    }

    private static void AddNamedIdMacros(
        GameCubeScriptMacroCatalog catalog,
        string parameterType,
        string prefix,
        IReadOnlyDictionary<int, string> names,
        string? zeroMacro = null)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(zeroMacro))
        {
            catalog.Add(parameterType, 0, "#" + zeroMacro);
            usedNames.Add(zeroMacro);
        }

        foreach (var (id, name) in names.OrderBy(pair => pair.Key))
        {
            if (id == 0 || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var baseName = prefix + "_" + MacroSafeName(name);
            var macroName = usedNames.Add(baseName)
                ? baseName
                : $"{baseName}_{id:0000}";
            catalog.Add(parameterType, unchecked((uint)id), "#" + macroName);
        }
    }

    private static void AddAbilityMacros(GameCubeScriptMacroCatalog catalog)
    {
        catalog.Add("ability", 0, "#ABILITY_NONE");
        for (var index = 1; index < Gen3AbilityNames.Length; index++)
        {
            catalog.Add("ability", unchecked((uint)index), "#ABILITY_" + MacroSafeName(Gen3AbilityNames[index]));
        }
    }

    private void AddTreasureMacros(GameCubeScriptMacroCatalog catalog)
    {
        try
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TREASURE_NONE" };
            catalog.Add("treasureID", 0, "#TREASURE_NONE");
            foreach (var treasure in LoadTreasureRecords().Where(treasure => treasure.Index > 0))
            {
                var baseName = "TREASURE_" + MacroSafeName(treasure.ItemName);
                var macroName = usedNames.Add(baseName)
                    ? baseName
                    : $"{baseName}_{treasure.Index:000}";
                catalog.Add("treasureID", unchecked((uint)treasure.Index), "#" + macroName);
            }
        }
        catch (Exception ex) when (IsParseException(ex) || ex is IOException)
        {
            catalog.Add("treasureID", 0, "#TREASURE_NONE");
        }
    }

    private void AddShadowPokemonMacros(GameCubeScriptMacroCatalog catalog)
    {
        try
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SHADOW_POKEMON_NONE" };
            catalog.Add("shadowID", 0, "#SHADOW_POKEMON_NONE");
            foreach (var shadow in LoadShadowPokemonRecords().Where(shadow => shadow.Index > 0))
            {
                var baseName = "SHADOW_" + MacroSafeName(shadow.SpeciesName);
                var macroName = usedNames.Add(baseName)
                    ? $"{baseName}_{shadow.Index:00}"
                    : $"{baseName}_{shadow.Index:00}";
                catalog.Add("shadowID", unchecked((uint)shadow.Index), "#" + macroName);
            }
        }
        catch (Exception ex) when (IsParseException(ex) || ex is IOException)
        {
            catalog.Add("shadowID", 0, "#SHADOW_POKEMON_NONE");
        }
    }

    private static string MacroSafeName(string value)
    {
        value = value
            .Replace("P$", "POKEDOLLARS", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00e9", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("\u2640", "F", StringComparison.Ordinal)
            .Replace("\u2642", "M", StringComparison.Ordinal);

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = true;
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        var text = builder.ToString().Trim('_');
        return text.Length == 0 ? "UNKNOWN" : text;
    }
}
