using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.OpenApi.Models;

namespace BackBuddy.Api.Service.Swagger
{
    public static class SwaggerConfigurationExtension
    {
        public static void ConfigureFullSwaggerConfig(this IServiceCollection col)
        {
            col.AddApiVersioning(
                options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1.0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = ApiVersionReader.Combine(
                           new UrlSegmentApiVersionReader(),
                           new QueryStringApiVersionReader("api-version"),
                           new HeaderApiVersionReader("X-Version"),
                           new MediaTypeApiVersionReader("x-version"));
                })
            .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
            .AddApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });

            col.AddEndpointsApiExplorer();

            col.AddSwaggerGen(option =>
            {
                // Add OpenAPI document with valid version
                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "BackBuddy API",
                    Version = "v1",
                    Description = "BackBuddy API",
                    Contact = new OpenApiContact
                    {
                        Name = "BackBuddy",
                    }
                });

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}
