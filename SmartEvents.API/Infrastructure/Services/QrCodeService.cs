using QRCoder;
using SmartEvents.API.Application.Interfaces;

namespace SmartEvents.API.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(10);
        return Convert.ToBase64String(pngBytes);
    }
}
