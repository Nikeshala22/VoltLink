// -----------------------------------------------------------------------------
// File        : SmtpSettings.cs
// Project     : VoltLink.Api - Smart Solar Microgrid Trading System
// Module      : Configuration
// Description : Strongly typed representation of the "Smtp" section in
//               appsettings.json for Gmail SMTP email dispatch.
// Author      : <IT Number - Member Name>
// Created     : 2026-10-09
// -----------------------------------------------------------------------------

namespace VoltLink.Api.Configuration;

/// <summary>
/// Configuration parameters for Gmail SMTP dispatch of two-factor OTP codes.
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// SMTP Server Host (default: smtp.gmail.com).
    /// </summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>
    /// SMTP Port (default: 587 for STARTTLS).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Enforces TLS/SSL encryption for secure transmission.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Sender Gmail address (e.g. yourname@gmail.com).
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the recipient's inbox.
    /// </summary>
    public string SenderName { get; set; } = "VoltLink Security";

    /// <summary>
    /// 16-character Google App Password generated from Google Account Security.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
