using Keycloak.Api.Hubs;
using Keycloak.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace Keycloak.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddCors();
        builder.Services.AddSignalR();
        builder.Services.AddScoped<IChatService, ChatService>();

        #region Keycloak Работающая версия 1 с использованием AddKeycloakAuthentication...
        //builder.Services.AddKeycloakAuthentication(new KeycloakAuthenticationOptions()
        //{
        //    AuthServerUrl = builder.Configuration["Keycloak:auth-server-url"]!,
        //    Realm = builder.Configuration["Keycloak:realm"]!,
        //    Resource = builder.Configuration["Keycloak:resource"]!,
        //    SslRequired = builder.Configuration["Keycloak:ssl-required"]!,
        //    VerifyTokenAudience = false,
        //});

        //builder.Services.AddKeycloakAuthorization(new KeycloakProtectionClientOptions()
        //{
        //    AuthServerUrl = builder.Configuration["Keycloak:auth-server-url"]!,
        //    Realm = builder.Configuration["Keycloak:realm"]!,
        //    Resource = builder.Configuration["Keycloak:resource"]!,
        //    SslRequired = builder.Configuration["Keycloak:ssl-required"]!,
        //    VerifyTokenAudience = false
        //});
        #endregion

        #region Keycloak Работающая версия 2 с использованием AddOpenIdConnect...
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, c =>
            {
                c.MetadataAddress = $"{builder.Configuration["Keycloak:auth-server-url"]}realms/{builder.Configuration["Keycloak:realm"]}/.well-known/openid-configuration";
                c.RequireHttpsMetadata = false;
                c.Authority = $"{builder.Configuration["Keycloak:auth-server-url"]}realms/{builder.Configuration["Keycloak:realm"]}";
                c.Audience = "account";
            })
            //можно и без нее, работает, он для более тонкой настройки
            .AddOpenIdConnect("Keycloak", options =>
            {
                options.Authority = $"{builder.Configuration["Keycloak:auth-server-url"]}realms/{builder.Configuration["Keycloak:realm"]}";
                options.ClientId = builder.Configuration["Keycloak:resource"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
                options.Scope.Add("roles");// Обязательно добавьте "roles"
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
                options.Events = new OpenIdConnectEvents
                {
                    OnUserInformationReceived = context =>
                    {
                        MapKeyCloakRolesToRoleClaims(context);
                        return Task.CompletedTask;
                    }
                };
                options.UsePkce = true;
                options.MapInboundClaims = true;
                //options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
        builder.Services.AddAuthorization();
        #endregion


        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Keycloak",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.OpenIdConnect,
                OpenIdConnectUrl = new Uri($"{builder.Configuration["Keycloak:auth-server-url"]}realms/{builder.Configuration["Keycloak:realm"]}/.well-known/openid-configuration"),
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };
            c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {securityScheme, Array.Empty<string>()}
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors(a => a.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

        //app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();
        app.MapHub<ChatHub>("chat");

        app.Run();
    }

    private static void MapKeyCloakRolesToRoleClaims(UserInformationReceivedContext context)
    {
        if (context.Principal.Identity is not ClaimsIdentity claimsIdentity) return;

        if (context.User.RootElement.TryGetProperty("preferred_username", out var username))
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, username.ToString()));
        }

        if (context.User.RootElement.TryGetProperty("realm_access", out var realmAccess)
            && realmAccess.TryGetProperty("roles", out var globalRoles))
        {
            foreach (var role in globalRoles.EnumerateArray())
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
            }
        }

        if (context.User.RootElement.TryGetProperty("resource_access", out var clientAccess)
            && clientAccess.TryGetProperty(context.Options.ClientId, out var client)
            && client.TryGetProperty("roles", out var clientRoles))
        {
            foreach (var role in clientRoles.EnumerateArray())
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
            }
        }
    }
}

