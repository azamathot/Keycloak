using Keycloak.Blazor;
using Keycloak.Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<JwtAuthorizationMessageHandler>();

builder.Services.AddHttpClient("API",
    client => client.BaseAddress = new Uri("https://localhost:8081/"))
  .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    options.ProviderOptions.Authority = builder.Configuration["Keycloak:auth-server-url"] + "/realms/" + builder.Configuration["Keycloak:realm"];
    options.ProviderOptions.ClientId = builder.Configuration["Keycloak:resource"];
    options.ProviderOptions.MetadataUrl = builder.Configuration["Keycloak:auth-server-url"] + "/realms/" + builder.Configuration["Keycloak:realm"] + "/.well-known/openid-configuration";
    options.ProviderOptions.ResponseType = "id_token token";
    options.UserOptions.RoleClaim = "roles";
    options.UserOptions.ScopeClaim = "scope";

});
builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<CustomUserFactory>();

await builder.Build().RunAsync();

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    public static string JwtToken { get; private set; }
    private readonly IAccessTokenProvider _accessTokenProvider;
    public JwtAuthorizationMessageHandler(IAccessTokenProvider accessTokenProvider)
    {
        _accessTokenProvider = accessTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Получение JWT-токена
        var accessTokenResult = await _accessTokenProvider.RequestAccessToken();

        // Проверка, получен ли токен 
        if (accessTokenResult.TryGetToken(out var accessToken))
        {
            JwtToken = accessToken.Value;
            // Добавление токена в заголовок Authorization (исправление)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
        }

        // Отправка запроса к API
        return await base.SendAsync(request, cancellationToken);
    }
}
