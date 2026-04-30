using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

internal static class ViewPerformanceDiagnostics
{
    public static void AttachFirstRenderLogs(Control control, string label)
    {
        var attachTimer = Stopwatch.StartNew();
        var renderTimer = Stopwatch.StartNew();
        var layoutTimer = Stopwatch.StartNew();
        var didLogLayout = false;

        control.AttachedToVisualTree += (_, _) =>
        {
            Log(control, $"{label} attached", attachTimer, CountVisuals(control));

            Dispatcher.UIThread.Post(
                () => Log(control, $"{label} first render queue", renderTimer, CountVisuals(control)),
                DispatcherPriority.Render);
        };

        control.LayoutUpdated += (_, _) =>
        {
            if (didLogLayout)
            {
                return;
            }

            didLogLayout = true;
            Log(control, $"{label} first layout", layoutTimer, CountVisuals(control));
        };
    }

    public static int CountVisuals(Visual visual)
        => visual.GetVisualDescendants().Count() + 1;

    private static void Log(Control control, string label, Stopwatch stopwatch, int visualCount)
    {
        if (control.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.LogPerformance(label, stopwatch, visualCount);
        }
    }
}
