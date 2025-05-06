using BackBuddy.Api.Service.V1.Auth.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackBuddy.Api.Service.V1.Auth.Extensions
{
    public static class WebApplicationBuilderExtension
    {

        public static void ConfigureAuthentification(this WebApplicationBuilder builder)
        {
            if(!builder.Environment.IsDevelopment())
            {
                Setup(builder);
            }
            else
            {
                SetupDev(builder);
            }
        }

        private static void Setup(WebApplicationBuilder builder)
        {
            IConfigurationSection authConfigSection = builder.Configuration.GetSection("Auth");
            AuthConfigDto authConfig = authConfigSection.Get<AuthConfigDto>() ?? throw new InvalidOperationException("Auth configuration is not set!");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = true;
                options.Authority = authConfig.Authority;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = authConfig.ValidIssuer,
                    ValidateAudience = true,
                    ValidAudience = authConfig.ValidAudience,
                    ValidateLifetime = true,
                };
            });
        }

        private static void SetupDev(WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context => Task.CompletedTask,

                    OnAuthenticationFailed = context =>
                    {
                        string token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                        if (string.IsNullOrEmpty(token))
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }

                        JwtSecurityTokenHandler handler = new();
                        if (!handler.CanReadToken(token))
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }

                        JwtSecurityToken jwt = handler.ReadJwtToken(token);

                        string? userId = jwt.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value
                                    ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }

                        List<Claim> claims = [new Claim("user_id", userId)];
                        foreach (var claim in jwt.Claims)
                        {
                            if (claim.Type != "user_id" && !claims.Any(c => c.Type == claim.Type))
                            {
                                claims.Add(claim);
                            }
                        }

                        ClaimsIdentity identity = new(claims, JwtBearerDefaults.AuthenticationScheme);
                        context.Principal = new ClaimsPrincipal(identity);

                        context.Success();
                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false,
                    RequireExpirationTime = false
                };
            });
        }

    }
}
