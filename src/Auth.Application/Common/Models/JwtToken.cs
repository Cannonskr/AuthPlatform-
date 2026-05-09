namespace Auth.Application.Common.Models;

public class JwtToken
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenId { get; set; } = string.Empty;
}
