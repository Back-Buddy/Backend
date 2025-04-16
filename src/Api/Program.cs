using BackBuddy.Api.Service.Swagger;
using BackBuddy.Api.Service.V1.Auth.Extensions;
using BackBuddy.Api.Service.V1.ExceptionHandlers;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureAuthentification();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AbstractBaseExceptionHandler>();

builder.Services.AddControllers();

builder.Services.ConfigureFullSwaggerConfig();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

await app.RunAsync();
