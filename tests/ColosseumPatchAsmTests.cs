using System.Reflection;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumPatchAsmTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    [Fact]
    public void PowerPcHelpersEncodeLegacyPatchInstructions()
    {
        Assert.Equal(0x38000000u, InvokeUInt32("Li", 0, 0));
        Assert.Equal(0x9005002cu, InvokeUInt32("Stw", 0, 5, 0x2c));
        Assert.Equal(0x4800000cu, InvokeUInt32("BranchForward", 0, 12));
        Assert.Equal(0x4bfbe970u, InvokeUInt32("Branch", 0x0dd970, 0x09c2e0));
        Assert.Equal(0x480f1f55u, InvokeUInt32("BranchLink", 0x5c70, 0xf7bc4));
        Assert.Equal(0x40820010u, InvokeUInt32("Bne", 0xbe360, 0xbe370));
        Assert.Equal(0x70601600u, InvokeUInt32("Andi", 0, 3, 0x1600u));
    }

    [Fact]
    public void ShiftedImmediateMatchesSwiftLowHalfCarry()
    {
        var result = InvokeTuple("LoadImmediateShifted32Bit", 5, 0x80408378u);

        Assert.Equal(0x3ca08041u, result.High);
        Assert.Equal(0x38a58378u, result.Low);
    }

    private static uint InvokeUInt32(string methodName, params object[] args)
    {
        var method = typeof(ColosseumCommonRel).GetMethod(methodName, PrivateStatic)
            ?? throw new MissingMethodException(nameof(ColosseumCommonRel), methodName);
        return (uint)(method.Invoke(null, args) ?? throw new InvalidOperationException($"{methodName} returned null."));
    }

    private static (uint High, uint Low) InvokeTuple(string methodName, params object[] args)
    {
        var method = typeof(ColosseumCommonRel).GetMethod(methodName, PrivateStatic)
            ?? throw new MissingMethodException(nameof(ColosseumCommonRel), methodName);
        return ((uint High, uint Low))(method.Invoke(null, args)
            ?? throw new InvalidOperationException($"{methodName} returned null."));
    }
}
