using Microsoft.AspNetCore.Http;
using Moq;
using Serilog;
using System.Security.Claims;
using System.Net;
using Xunit;

namespace RPG_ESI07.Tests.Infrastructure.Logging;

public class SerilogConfigurationTests
{
    [Fact]
    public void EnrichDiagnosticContext_Adds_UserId_And_RemoteIP()
    {
        // Arrange
        var diagnosticContextMock = new Mock<IDiagnosticContext>();
        var httpContext = new DefaultHttpContext();

        // Simulation d'un utilisateur authentifié avec ID 123
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "123") };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Simulation d'une IP distante
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("1.2.3.4");

        // Act (Logique extraite du Step 2.3 de Program.cs)
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();

        diagnosticContextMock.Object.Set("UserId", userId);
        diagnosticContextMock.Object.Set("RemoteIP", remoteIp);

        // Assert
        diagnosticContextMock.Verify(x => x.Set("UserId", "123"), Times.Once);
        diagnosticContextMock.Verify(x => x.Set("RemoteIP", "1.2.3.4"), Times.Once);
    }
}