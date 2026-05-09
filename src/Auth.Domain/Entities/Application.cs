using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class Application : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string ApiKey { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<Role> Roles { get; private set; } = new List<Role>();

    private Application() { }

    public Application(string name, string code, string? description, string apiKey)
        : this(default, name, code, description, apiKey)
    {
    }

    public Application(Guid id, string name, string code, string? description, string apiKey)
        : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        ApiKey = apiKey;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RotateApiKey(string newApiKey)
    {
        ApiKey = newApiKey;
        UpdatedAt = DateTime.UtcNow;
    }
}
