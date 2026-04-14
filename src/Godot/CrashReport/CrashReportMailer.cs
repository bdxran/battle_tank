using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.CrashReport;

public class CrashReportMailer
{
    private const string Recipient = "randy.blondiaux@contraste.com";

    public async Task<bool> SendAsync(string report, string playerComment)
    {
        var smtpHost = System.Environment.GetEnvironmentVariable("SMTP_HOST");
        if (string.IsNullOrEmpty(smtpHost))
        {
            GD.Print("[CrashReportMailer] SMTP_HOST not configured — skipping mail send");
            return false;
        }

        var smtpPortStr = System.Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
        var smtpUser = System.Environment.GetEnvironmentVariable("SMTP_USER") ?? "";
        var smtpPass = System.Environment.GetEnvironmentVariable("SMTP_PASS") ?? "";

        if (!int.TryParse(smtpPortStr, out int smtpPort))
            smtpPort = 587;

        try
        {
            var subject = $"[BattleTank] Crash Report v{Constants.GameVersion} — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            var body = string.IsNullOrWhiteSpace(playerComment)
                ? report
                : $"=== Player Comment ===\n{playerComment}\n\n{report}";

            using var message = new MailMessage(smtpUser, Recipient, subject, body);
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            await client.SendMailAsync(message);
            GD.Print("[CrashReportMailer] Report sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CrashReportMailer] Send failed: {ex.Message}");
            return false;
        }
    }
}
