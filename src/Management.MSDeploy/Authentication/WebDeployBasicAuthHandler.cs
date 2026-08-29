namespace Xylab.Management.WebDeploy.Authentication;

using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class WebDeployBasicAuthOptions
{
    [Required]
    public string? Username { get; set; }

    [Required]
    public string? Password { get; set; }
}

public class WebDeployBasicAuthHandler(
    IOptions<WebDeployBasicAuthOptions> options)
    : IWebDeployAuthenticationHandler
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<WebDeployBasicAuthOptions>().ValidateDataAnnotations().ValidateOnStart();
    }

    public ValueTask<bool> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var authentication)
            || !string.Equals(authentication.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(authentication.Parameter))
        {
            return ValueTask.FromResult(false);
        }

        byte[] encodedCredentials;
        try
        {
            encodedCredentials = Convert.FromBase64String(authentication.Parameter);
        }
        catch (FormatException)
        {
            return ValueTask.FromResult(false);
        }

        var credentials = Encoding.UTF8.GetString(encodedCredentials);
        var separator = credentials.IndexOf(':');
        if (separator < 0)
        {
            return ValueTask.FromResult(false);
        }

        bool authResult =
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(credentials[..separator]),
                Encoding.UTF8.GetBytes(options.Value.Username!)) &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(credentials[(separator + 1)..]),
                Encoding.UTF8.GetBytes(options.Value.Password!));

        return ValueTask.FromResult(authResult);
    }

    public ValueTask ChallengeAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        response.Headers.WWWAuthenticate = "Basic realm=\"WebDeploy\"";
        response.StatusCode = StatusCodes.Status401Unauthorized;
        return ValueTask.CompletedTask;
    }
}
