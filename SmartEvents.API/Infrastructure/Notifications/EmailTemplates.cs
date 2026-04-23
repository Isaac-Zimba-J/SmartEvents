using SmartEvents.API.Domain.Entities;

namespace SmartEvents.API.Infrastructure.Notifications;

public static class EmailTemplates
{
    private const string PrimaryColor = "#6c47ff";
    private const string BgColor = "#f4f4f7";
    private const string CardBg = "#ffffff";

    public static string RegistrationConfirmed(string firstName, Event ev, Ticket? ticket, bool promoted = false)
    {
        var heading = promoted
            ? "You've got a spot!"
            : "Registration Confirmed";

        var intro = promoted
            ? $"Great news, {firstName}! A spot opened up and your registration for <strong>{ev.Title}</strong> is now confirmed."
            : $"Hi {firstName}, you're all set! Your registration for <strong>{ev.Title}</strong> has been confirmed.";

        var ticketSection = ticket is not null
            ? $"""
              <tr>
                <td style="padding:16px;background:#f9f6ff;border-radius:8px;text-align:center;">
                  <p style="margin:0;font-size:13px;color:#888;">Your Ticket Number</p>
                  <p style="margin:8px 0 0;font-size:22px;font-weight:700;color:{PrimaryColor};letter-spacing:2px;">{ticket.TicketNumber}</p>
                  {(ticket.QrCode.Length > 0 ? $"<img src=\"data:image/png;base64,{ticket.QrCode}\" alt=\"QR Code\" style=\"margin-top:12px;width:120px;height:120px;\" />" : "")}
                </td>
              </tr>
              """
            : "";

        return Wrap(heading, $"""
            <p style="font-size:15px;color:#444;">{intro}</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:12px;border-left:4px solid {PrimaryColor};background:#fafafa;">
                  <p style="margin:0;font-size:13px;color:#888;">Event</p>
                  <p style="margin:4px 0 0;font-size:15px;font-weight:600;color:#222;">{ev.Title}</p>
                </td>
              </tr>
              <tr><td style="height:8px;"></td></tr>
              <tr>
                <td style="padding:12px;border-left:4px solid {PrimaryColor};background:#fafafa;">
                  <p style="margin:0;font-size:13px;color:#888;">Date &amp; Time</p>
                  <p style="margin:4px 0 0;font-size:15px;color:#222;">{ev.StartDate:dddd, MMMM d, yyyy} at {ev.StartDate:h:mm tt}</p>
                </td>
              </tr>
              <tr><td style="height:8px;"></td></tr>
              {ticketSection}
            </table>
            <p style="font-size:14px;color:#666;">Keep this email handy — you may need your ticket number at the door.</p>
            """);
    }

    public static string Waitlisted(string firstName, Event ev, int? position)
    {
        var posText = position.HasValue ? $"You are currently <strong>#{position}</strong> on the waitlist." : "You are on the waitlist.";

        return Wrap("You're on the Waitlist", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, <strong>{ev.Title}</strong> is currently full but we've added you to the waitlist.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:16px;background:#fff8e1;border-radius:8px;text-align:center;">
                  <p style="margin:0;font-size:15px;color:#555;">{posText}</p>
                  <p style="margin:8px 0 0;font-size:13px;color:#888;">We'll email you immediately if a spot opens up.</p>
                </td>
              </tr>
            </table>
            <p style="font-size:14px;color:#666;">Event: <strong>{ev.Title}</strong> &mdash; {ev.StartDate:MMMM d, yyyy}</p>
            """);
    }

    public static string EventReminder(string firstName, Event ev)
    {
        return Wrap($"Reminder: {ev.Title}", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, just a reminder that <strong>{ev.Title}</strong> is coming up soon!</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:12px;border-left:4px solid {PrimaryColor};background:#fafafa;">
                  <p style="margin:0;font-size:13px;color:#888;">Date</p>
                  <p style="margin:4px 0 0;font-size:15px;color:#222;">{ev.StartDate:dddd, MMMM d, yyyy} at {ev.StartDate:h:mm tt}</p>
                </td>
              </tr>
            </table>
            <p style="font-size:14px;color:#666;">We look forward to seeing you there!</p>
            """);
    }

    public static string EventCancelled(string firstName, Event ev)
    {
        return Wrap($"Event Cancelled: {ev.Title}", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, we're sorry to inform you that <strong>{ev.Title}</strong> has been cancelled.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:16px;background:#fff3f3;border-radius:8px;text-align:center;">
                  <p style="margin:0;font-size:15px;color:#c0392b;font-weight:600;">This event has been cancelled</p>
                  <p style="margin:8px 0 0;font-size:13px;color:#888;">If you made a payment, a refund will be processed within 5–7 business days.</p>
                </td>
              </tr>
            </table>
            <p style="font-size:14px;color:#666;">We apologize for the inconvenience. We hope to see you at future events.</p>
            """);
    }

    public static string EventUpdated(string firstName, Event ev)
    {
        return Wrap($"Event Updated: {ev.Title}", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, the details for <strong>{ev.Title}</strong> have been updated.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:12px;border-left:4px solid {PrimaryColor};background:#fafafa;">
                  <p style="margin:0;font-size:13px;color:#888;">New Date</p>
                  <p style="margin:4px 0 0;font-size:15px;color:#222;">{ev.StartDate:dddd, MMMM d, yyyy} at {ev.StartDate:h:mm tt}</p>
                </td>
              </tr>
            </table>
            <p style="font-size:14px;color:#666;">Please review the updated details and let us know if you have any questions.</p>
            """);
    }

    public static string CheckInConfirmed(string firstName, Event ev)
    {
        return Wrap($"Welcome to {ev.Title}!", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, your check-in for <strong>{ev.Title}</strong> has been confirmed.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td style="padding:16px;background:#f0fdf4;border-radius:8px;text-align:center;">
                  <p style="margin:0;font-size:22px;">&#10003;</p>
                  <p style="margin:8px 0 0;font-size:15px;font-weight:600;color:#16a34a;">Successfully Checked In</p>
                  <p style="margin:4px 0 0;font-size:13px;color:#888;">{ev.StartDate:MMMM d, yyyy}</p>
                </td>
              </tr>
            </table>
            <p style="font-size:14px;color:#666;">Enjoy the event!</p>
            """);
    }

    public static string VerificationEmail(string firstName, string verificationLink)
    {
        return Wrap("Verify your email address", $"""
            <p style="font-size:15px;color:#444;">Hi {firstName}, thanks for joining SmartEvents! Please verify your email address to unlock all features.</p>
            <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;">
              <tr>
                <td align="center">
                  <a href="{verificationLink}"
                     style="display:inline-block;padding:14px 32px;background:{PrimaryColor};color:#fff;text-decoration:none;border-radius:8px;font-size:15px;font-weight:600;">
                    Verify Email Address
                  </a>
                </td>
              </tr>
            </table>
            <p style="font-size:13px;color:#888;">This link expires in 24 hours. If you didn't create an account, you can safely ignore this email.</p>
            <p style="font-size:12px;color:#aaa;word-break:break-all;">Or copy this link: {verificationLink}</p>
            """);
    }

    // ---

    private static string Wrap(string title, string body)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:{BgColor};font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:{BgColor};padding:40px 0;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="background:{CardBg};border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                      <!-- Header -->
                      <tr>
                        <td style="background:{PrimaryColor};padding:28px 32px;">
                          <p style="margin:0;font-size:20px;font-weight:700;color:#fff;">SmartEvents</p>
                          <p style="margin:6px 0 0;font-size:16px;color:rgba(255,255,255,0.85);">{title}</p>
                        </td>
                      </tr>
                      <!-- Body -->
                      <tr>
                        <td style="padding:32px;">
                          {body}
                        </td>
                      </tr>
                      <!-- Footer -->
                      <tr>
                        <td style="padding:20px 32px;background:#f9f9fb;border-top:1px solid #eee;text-align:center;">
                          <p style="margin:0;font-size:12px;color:#aaa;">&copy; {DateTime.UtcNow.Year} SmartEvents. All rights reserved.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
