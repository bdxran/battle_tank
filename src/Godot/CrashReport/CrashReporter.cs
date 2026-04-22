using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Settings;

namespace BattleTank.Godot.CrashReport;

public class CrashReporter
{
    private const int LogBufferSize = 50;
    private const string PendingSuffix = ".pending";

    private readonly Queue<string> _logBuffer = new();
    private Func<string>? _getGamePhase;
    private string _reportsDir = "";

    public event Action<string[]>? PendingReportsFound;

    public void Initialize(Func<string> getGamePhase)
    {
        _getGamePhase = getGamePhase;
        _reportsDir = AppPaths.CrashReportsDir;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public void CheckPendingReports()
    {
        var pending = GetPendingReportPaths();
        if (pending.Length > 0)
            PendingReportsFound?.Invoke(pending);
    }

    public void LogLine(string line)
    {
        lock (_logBuffer)
        {
            if (_logBuffer.Count >= LogBufferSize)
                _logBuffer.Dequeue();
            _logBuffer.Enqueue(line);
        }
    }

    public string[] GetPendingReportPaths()
    {
        if (!Directory.Exists(_reportsDir))
            return Array.Empty<string>();
        return Directory.GetFiles(_reportsDir, "*" + PendingSuffix);
    }

    public string ReadReport(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

    public void MarkReportSent(string path)
    {
        if (!File.Exists(path)) return;
        var sent = path.Replace(PendingSuffix, ".sent");
        File.Move(path, sent, overwrite: true);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exceptionText = e.ExceptionObject?.ToString() ?? "Unknown exception";
        var report = GenerateReport(exceptionText);
        SaveReportLocally(report);
    }

    private string GenerateReport(string exceptionText)
    {
        var phase = _getGamePhase?.Invoke() ?? "Unknown";
        var os = OS.GetName();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        string logs;
        lock (_logBuffer)
            logs = string.Join("\n", _logBuffer);

        return $"""
            === BattleTank Crash Report ===
            Timestamp : {timestamp}
            Version   : {Constants.GameVersion}
            OS        : {os}
            Phase     : {phase}

            === Exception ===
            {exceptionText}

            === Last Logs ({LogBufferSize} lines) ===
            {logs}
            """;
    }

    private void SaveReportLocally(string report)
    {
        try
        {
            var filename = $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss}{PendingSuffix}";
            var path = Path.Combine(_reportsDir, filename);
            File.WriteAllText(path, report);
            GD.PrintErr($"[CrashReporter] Report saved: {path}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CrashReporter] Failed to save report: {ex.Message}");
        }
    }
}
