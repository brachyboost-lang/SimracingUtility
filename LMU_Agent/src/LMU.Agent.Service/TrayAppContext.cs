using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LMU.Agent.Core.Services;
using Microsoft.Extensions.Hosting;

namespace LMU.Agent.Service;

/// <summary>
/// Tray-Oberfläche des Agents: zeigt ein Symbol im Infobereich mit Kontextmenü
/// zum Öffnen der LMU-Ordner und zum Beenden des Diensts.
/// </summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly IHost _host;
    private readonly TelemetryServer _telemetry;
    private readonly string _resultsPath;
    private readonly NotifyIcon _icon;

    public TrayAppContext(IHost host, TelemetryServer telemetry, string resultsPath)
    {
        _host = host;
        _telemetry = telemetry;
        _resultsPath = resultsPath;

        var menu = new ContextMenuStrip();
        menu.Items.Add("LMU Agent läuft").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Ergebnis-Ordner öffnen", null, (_, _) => OpenFolder(_resultsPath));
        menu.Items.Add("Telemetrie-Ordner öffnen", null,
            (_, _) => OpenFolder(TelemetryLocator.GameLogFolderFromResults(_resultsPath)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => Exit());

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "LMU Agent",
            Visible = true,
            ContextMenuStrip = menu,
        };
    }

    private static void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            MessageBox.Show($"Ordner nicht gefunden:\n{path}", "LMU Agent",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void Exit()
    {
        _icon.Visible = false;
        try { _telemetry.Dispose(); } catch { /* egal beim Beenden */ }
        try { _host.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); } catch { }
        _host.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _icon.Dispose();
        base.Dispose(disposing);
    }
}
