using Auth.Application.Common.Models;

namespace Auth.Application.Common.Interfaces;

public interface IJwtService
{
    Task<JwtToken> GenerateTokenAsync(Guid userId, string email, string fullName,
        IList<string> roles, IList<string> permissions, string applicationCode, string? tenantId);
    Task<Guid?> ValidateTokenAsync(string token);
}
