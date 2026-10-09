// -----------------------------------------------------------------------------
// File        : EmailService.cs
// Project     : VoltLink.Api - Smart Solar Microgrid Trading System
// Module      : Services
// Description : Delivers transactional and authentication emails via Gmail SMTP.
//               Includes enterprise HTML templates and graceful fallback logging
//               if SMTP credentials are not yet configured or network is offline.
// Author      : <IT Number - Member Name>
// Created     : 2026-10-09
// -----------------------------------------------------------------------------

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using VoltLink.Api.Configuration;

namespace VoltLink.Api.Services;

/// <summary>
/// Dispatches transactional emails via Gmail SMTP (smtp.gmail.com:587) with STARTTLS.
/// </summary>
public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<SmtpSettings> options,
        ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends a branded 2FA verification email containing the 6-digit OTP passcode.
    /// </summary>
    public async Task SendTwoFactorCodeAsync(
        string toEmail,
        string recipientName,
        string code,
        CancellationToken cancellationToken = default)
    {
        // If SMTP credentials have not been configured yet, log a clear advisory and exit gracefully.
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning(
                "[Gmail SMTP Pending Configuration] 2FA verification code for {Email} ({Name}) is: {Code}. " +
                "To deliver real emails, set Smtp:SenderEmail and Smtp:Password (Google App Password) in appsettings.json.",
                toEmail, recipientName, code);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.SenderEmail.Trim(), _settings.Password.Trim()),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000 // 15-second timeout
            };

            var fromAddress = new MailAddress(_settings.SenderEmail.Trim(), _settings.SenderName);
            var toAddress = new MailAddress(toEmail.Trim(), recipientName);

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = $"{code} is your VoltLink verification code",
                IsBodyHtml = true,
                Body = BuildHtmlEmailBody(recipientName, code)
            };

            // Include plain text fallback
            var plainTextView = AlternateView.CreateAlternateViewFromString(
                $"Hello {recipientName},\n\nYour VoltLink 2FA verification code is: {code}\n\n" +
                "This code will expire in 10 minutes.\nIf you did not request this code, please secure your account immediately.\n\n" +
                "— VoltLink Microgrid Security Team",
                null,
                "text/plain");
            message.AlternateViews.Add(plainTextView);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("2FA verification email sent successfully to {Email} via Gmail SMTP.", toEmail);
        }
        catch (Exception ex)
        {
            // Log the error with high detail, but do not throw so the login/verify process doesn't 500
            _logger.LogError(
                ex,
                "[Gmail SMTP Failure] Could not send 2FA email to {Email}. Code: {Code}. Reason: {Message}",
                toEmail, code, ex.Message);
        }
    }

    /// <summary>
    /// Generates a responsive, enterprise-grade HTML email template.
    /// </summary>
    private static string BuildHtmlEmailBody(string recipientName, string code)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>VoltLink Security Code</title>
  <style>
    body {{
      margin: 0;
      padding: 0;
      background-color: #f1f5f9;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      color: #0f172a;
    }}
    .wrapper {{
      width: 100%;
      table-layout: fixed;
      background-color: #f1f5f9;
      padding: 40px 16px;
    }}
    .container {{
      max-width: 540px;
      margin: 0 auto;
      background-color: #ffffff;
      border-radius: 16px;
      overflow: hidden;
      border: 1px solid #e2e8f0;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05);
    }}
    .header {{
      background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
      padding: 32px 28px;
      text-align: center;
    }}
    .header h1 {{
      margin: 0;
      color: #ffffff;
      font-size: 22px;
      font-weight: 700;
      letter-spacing: -0.02em;
    }}
    .header p {{
      margin: 6px 0 0;
      color: #bae6fd;
      font-size: 13px;
    }}
    .content {{
      padding: 36px 32px;
    }}
    .greeting {{
      font-size: 16px;
      font-weight: 600;
      color: #0f172a;
      margin-bottom: 12px;
    }}
    .message {{
      font-size: 14px;
      line-height: 1.6;
      color: #475569;
      margin-bottom: 24px;
    }}
    .code-box {{
      background: #f0f9ff;
      border: 1.5px dashed #0284c7;
      border-radius: 12px;
      padding: 20px;
      text-align: center;
      margin: 24px 0;
    }}
    .code-label {{
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #0369a1;
      margin-bottom: 8px;
    }}
    .code-number {{
      font-size: 34px;
      font-family: 'Courier New', Courier, monospace;
      font-weight: 700;
      letter-spacing: 8px;
      color: #0284c7;
      margin: 0;
    }}
    .warning {{
      background-color: #fffbeb;
      border-left: 4px solid #f59e0b;
      padding: 12px 16px;
      border-radius: 6px;
      font-size: 12.5px;
      color: #92400e;
      line-height: 1.5;
      margin-top: 24px;
    }}
    .footer {{
      padding: 24px 32px;
      background-color: #f8fafc;
      border-top: 1px solid #e2e8f0;
      text-align: center;
      font-size: 12px;
      color: #94a3b8;
      line-height: 1.5;
    }}
  </style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""container"">
      <div class=""header"">
        <h1>⚡ VoltLink Microgrid</h1>
        <p>Smart Solar Microgrid Trading Platform</p>
      </div>
      <div class=""content"">
        <div class=""greeting"">Hello {WebUtility.HtmlEncode(recipientName)},</div>
        <p class=""message"">
          A sign-in attempt was initiated for your VoltLink account. Please use the verification code below to complete your two-factor authentication.
        </p>
        <div class=""code-box"">
          <div class=""code-label"">Your 6-Digit Passcode</div>
          <div class=""code-number"">{code}</div>
        </div>
        <div class=""warning"">
          ⏱ <strong>Valid for 10 minutes.</strong> Never share this verification code with anyone. VoltLink support will never ask for your 2FA passcode.
        </div>
      </div>
      <div class=""footer"">
        &copy; {DateTime.UtcNow.Year} VoltLink Smart Solar Microgrid System.<br>
        This is an automated system notification. Please do not reply directly to this email.
      </div>
    </div>
  </div>
</body>
</html>
";
    }
}
