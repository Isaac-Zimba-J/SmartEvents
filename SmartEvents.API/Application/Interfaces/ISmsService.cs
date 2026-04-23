namespace SmartEvents.API.Application.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}
