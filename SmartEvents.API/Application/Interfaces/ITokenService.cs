using SmartEvents.API.Domain.Entities;

namespace SmartEvents.API.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
