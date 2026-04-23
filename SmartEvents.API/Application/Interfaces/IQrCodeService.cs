namespace SmartEvents.API.Application.Interfaces;

public interface IQrCodeService
{
    /// <summary>Returns a base64-encoded PNG of the QR code.</summary>
    string GenerateBase64(string content);
}
