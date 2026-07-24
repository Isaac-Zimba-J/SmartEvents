using SmartEvents.API.Application.DTOs;

namespace SmartEvents.API.Application.Interfaces;

public interface IPawaPayService
{
    Task<PawaPayInitiateResponse> InitiateDepositAsync(
        string depositId,
        decimal amount,
        string currency,
        string correspondent,
        string phoneNumber
    );

    Task<PawaPayDepositStatusResponse> GetDepositStatusAsync(string depositId);
}
