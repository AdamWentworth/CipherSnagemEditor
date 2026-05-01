namespace CipherSnagemEditor.Core.GameCube;

public sealed class GameCubeScriptMacroCatalog
{
    private readonly Dictionary<(string ParameterType, uint Value), GameCubeScriptMacro> _byTypeValue = new();
    private readonly Dictionary<string, GameCubeScriptMacro> _byName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<GameCubeScriptMacro> Macros => _byName.Values;

    public void Add(string parameterType, uint value, string macroName)
    {
        parameterType = NormalizeParameterType(parameterType);
        macroName = NormalizeMacroName(macroName);
        if (string.IsNullOrWhiteSpace(parameterType) || macroName.Length <= 1)
        {
            return;
        }

        var macro = new GameCubeScriptMacro(parameterType, value, macroName);
        _byTypeValue.TryAdd((parameterType, value), macro);
        _byName.TryAdd(macroName, macro);
    }

    public bool TryFormat(string parameterType, uint value, out GameCubeScriptMacro macro)
    {
        parameterType = NormalizeParameterType(parameterType);
        return _byTypeValue.TryGetValue((parameterType, value), out macro!);
    }

    public bool TryResolve(string macroName, out uint value)
    {
        macroName = NormalizeMacroName(macroName);
        if (_byName.TryGetValue(macroName, out var macro))
        {
            value = macro.Value;
            return true;
        }

        value = 0;
        return false;
    }

    public static string NormalizeMacroName(string macroName)
    {
        macroName = macroName.Trim();
        return macroName.StartsWith('#') ? macroName : "#" + macroName;
    }

    public static string NormalizeParameterType(string parameterType)
    {
        parameterType = parameterType.Trim();
        return parameterType.StartsWith('.') ? parameterType[1..] : parameterType;
    }
}

public sealed record GameCubeScriptMacro(string ParameterType, uint Value, string Name);
