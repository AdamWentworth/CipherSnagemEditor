using System.Reflection;
using CipherSnagemEditor.XD;

namespace CipherSnagemEditor.Tests;

public sealed class XdPatchAsmTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    [Fact]
    public void PowerPcHelpersEncodeXdShinyHueInstructions()
    {
        Assert.Equal(0x3ba00001u, InvokeUInt32("Li", 29, 1));
        Assert.Equal(0x83da0094u, InvokeUInt32("Lwz", 30, 26, 0x94));
        Assert.Equal(0x7c7b19d6u, InvokeUInt32("Mullw", 3, 27, 3));
        Assert.Equal(0x7c641bd6u, InvokeUInt32("Divw", 3, 4, 3));
        Assert.Equal(0x7c832050u, InvokeUInt32("Sub", 4, 4, 3));
        Assert.Equal(0x98fd0010u, InvokeUInt32("Stb", 7, 29, 0x10));
        Assert.Equal(0x70e70001u, InvokeUInt32("Andi", 7, 7, 1u));
        Assert.Equal(0x7c074000u, InvokeUInt32("Cmpw", 7, 8));
        Assert.Equal(0x41820020u, InvokeUInt32("Beq", 0x100L, 0x120L));
        Assert.Equal(0x40810020u, InvokeUInt32("Ble", 0x100L, 0x120L));
        Assert.Equal(0x41810020u, InvokeUInt32("Bgt", 0x100L, 0x120L));
    }

    [Fact]
    public void ShinyHueRoutineMatchesLegacyHookShape()
    {
        const long routineRamOffset = 0x0d3950;
        var routine = InvokeUInt32Array("BuildXdShinyHueRoutine", routineRamOffset);

        Assert.True(routine.Length > 100);
        Assert.Equal(0x38600000u, routine[0]);
        Assert.Equal(0x38800002u, routine[1]);
        Assert.Equal(0x7c7b19d6u, routine[4]);
        Assert.Equal(0x41820030u, routine[30]);
        Assert.Equal(0x83da0094u, routine[^2]);
        Assert.Equal(InvokeUInt32("Branch", routineRamOffset + ((routine.Length - 1) * 4), 0x801d9108L), routine[^1]);
    }

    private static uint InvokeUInt32(string methodName, params object[] args)
    {
        var method = typeof(XdProjectContext).GetMethod(methodName, PrivateStatic)
            ?? throw new MissingMethodException(nameof(XdProjectContext), methodName);
        return (uint)(method.Invoke(null, args) ?? throw new InvalidOperationException($"{methodName} returned null."));
    }

    private static uint[] InvokeUInt32Array(string methodName, params object[] args)
    {
        var method = typeof(XdProjectContext).GetMethod(methodName, PrivateStatic)
            ?? throw new MissingMethodException(nameof(XdProjectContext), methodName);
        return (uint[])(method.Invoke(null, args) ?? throw new InvalidOperationException($"{methodName} returned null."));
    }
}
