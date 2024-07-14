# Keycloak
Взаимодействие Blazor Wasm (SPA) с Авторизацией по ролям на Keycloak и отправляющий запрос на микросервис на Asp.Net Core Web Api, где также настроена авторизация. Пример полностью рабочий. После создания Realm на Keycloak для справной работы авторизации по ролям на RBAC, необходимо настроить на Keycloak отправку этих данных https://stackoverflow.com/questions/56327794/role-based-authorization-using-keycloak-and-net-core

#Blazor

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

            
            public class JwtAuthorizationMessageHandler : AuthorizationMessageHandler
            {
                public JwtAuthorizationMessageHandler(IAccessTokenProvider provider,
                  NavigationManager navigation)
                  : base(provider, navigation)
                {
                    ConfigureHandler(authorizedUrls: new[] { "https://localhost:8081" });
                }
            }


    public class CustomUserFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        public CustomUserFactory(IAccessTokenProviderAccessor accessor)
            : base(accessor)
        {
        }

        public async override ValueTask<ClaimsPrincipal> CreateUserAsync(
            RemoteUserAccount account,
            RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);
            var claimsIdentity = (ClaimsIdentity?)user.Identity;

            if (account != null && claimsIdentity != null)
            {
                MapArrayClaimsToMultipleSeparateClaims(account, claimsIdentity);
            }

            return user;
        }

        private void MapArrayClaimsToMultipleSeparateClaims(RemoteUserAccount account, ClaimsIdentity claimsIdentity)
        {
            foreach (var prop in account.AdditionalProperties)
            {
                var key = prop.Key;
                var value = prop.Value;
                if (value != null && (value is JsonElement element && element.ValueKind == JsonValueKind.Array))
                {
                    // Remove the Roles claim with an array value and create a separate one for each role.
                    claimsIdentity.RemoveClaim(claimsIdentity.FindFirst(prop.Key));
                    var claims = element.EnumerateArray().Select(x => new Claim(prop.Key, x.ToString()));
                    claimsIdentity.AddClaims(claims);
                }
            }
        }
    }

#Asp.Net Core Web Api

При использовании библиотеки KeycloakAuthentication и т.д. нужно использовать версию 1.6, т.к. с новыми версиями этот код ломается, т.е. обновления сломают ваш код, поэтому есть решение 2, которое не поломает код из-за обновлений.

        builder.Services.AddCors();
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
                c.Authority = $"{builder.Configuration["Keycloak:auth-server-url"]}realms/{builder.Configuration["Keycloak:realm"]}/account";
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


