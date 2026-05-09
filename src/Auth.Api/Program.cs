using System.Security.Cryptography;
using Auth.Api.Extensions;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Validate required configuration at startup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var issuer = jwtSettings["Issuer"];
var privateKeyPath = jwtSettings["PrivateKeyPath"];
var publicKeyPath = jwtSettings["PublicKeyPath"];
var privateKeyB64 = jwtSettings["PrivateKey"];
var publicKeyB64 = jwtSettings["PublicKey"];

if (string.IsNullOrWhiteSpace(issuer))
    throw new InvalidOperationException("JwtSettings:Issuer must be configured.");

var hasPrivateKey = (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
    || !string.IsNullOrEmpty(privateKeyB64);
var hasPublicKey = (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
    || !string.IsNullOrEmpty(publicKeyB64);

if (!hasPrivateKey)
    throw new InvalidOperationException(
        "JWT private key must be configured. Set JwtSettings:PrivateKeyPath or JwtSettings:PrivateKey.");

if (!hasPublicKey)
    throw new InvalidOperationException(
        "JWT public key must be configured. Set JwtSettings:PublicKeyPath or JwtSettings:PublicKey.");

// Validate keys are valid RSA
if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
{
    try { using var rsa = RSA.Create(); rsa.ImportFromPem(File.ReadAllText(privateKeyPath)); }
    catch (Exception ex) { throw new InvalidOperationException("Invalid RSA private key PEM file.", ex); }
}
if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
{
    try { using var rsa = RSA.Create(); rsa.ImportFromPem(File.ReadAllText(publicKeyPath)); }
    catch (Exception ex) { throw new InvalidOperationException("Invalid RSA public key PEM file.", ex); }
}

// Add layer services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// Add API services (controllers, cors, health checks)
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

// Add Swagger with JWT Bearer authentication support
builder.Services.AddSwaggerWithJwtAuth(builder.Environment);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseApiMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Initialize database (apply migrations and seed data)
await app.InitializeDatabaseAsync();

app.Run();
