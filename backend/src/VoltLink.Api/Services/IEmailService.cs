// -----------------------------------------------------------------------------
// File        : IEmailService.cs
// Project     : VoltLink.Api - Smart Solar Microgrid Trading System
// Module      : Services
// Description : Contract for delivering email notifications and security tokens.
// Author      : <IT Number - Member Name>
// Created     : 2026-10-09
// -----------------------------------------------------------------------------

namespace VoltLink.Api.Services;

/// <summary>
/// Service contract for sending outgoing emails such as 2FA OTP codes.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a 6-digit 2FA verification code to the recipient's email address.
    /// </summary>
    /// <param name="toEmail">The user's registered email address.</param>
    /// <param name="recipientName">The user's full name.</param>
    /// <param name="code">The generated 6-digit one-time passcode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTwoFactorCodeAsync(string toEmail, string recipientName, string code, CancellationToken cancellationToken = default);
}
