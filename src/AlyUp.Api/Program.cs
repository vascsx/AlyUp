using AlyUp.Api.Filters;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Admin;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Application.UseCases.ProfessionalAvailability;
using AlyUp.Application.UseCases.Professionals;
using AlyUp.Application.UseCases.Services;
using AlyUp.Application.Validators;
using AlyUp.Infrastructure.Data;
using AlyUp.Infrastructure.Extensions;
using AlyUp.Infrastructure.Middleware;
using AlyUp.Infrastructure.Repositories;
using AlyUp.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("AlyUp.Api");
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
        }));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    foreach (var knownProxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        if (IPAddress.TryParse(knownProxy, out var proxyAddress))
        {
            options.KnownProxies.Add(proxyAddress);
        }
    }
});

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAppAuthorization();
builder.Services.AddAppRateLimiting();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
builder.Services.AddScoped<ISalonRepository, SalonRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IProfessionalAvailabilityRepository, ProfessionalAvailabilityRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
builder.Services.AddScoped<IAccessTokenLifetimeProvider, AccessTokenLifetimeProvider>();
builder.Services.AddScoped<IRefreshTokenLifetimeProvider, RefreshTokenLifetimeProvider>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGeneratorService>();
builder.Services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
builder.Services.AddScoped<IInputNormalizer, InputNormalizer>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IAccessScopeService, AccessScopeService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<ValidationActionFilter>();

builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterClientRequestDto>, RegisterClientRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateSalonOwnerRequestDto>, CreateSalonOwnerRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateProfessionalRequestDto>, CreateProfessionalRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateServiceRequestDto>, CreateServiceRequestDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateServiceRequestDto>, UpdateServiceRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateProfessionalAvailabilityRequestDto>, CreateProfessionalAvailabilityRequestDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateProfessionalAvailabilityRequestDto>, UpdateProfessionalAvailabilityRequestDtoValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenRequestDto>, RefreshTokenRequestDtoValidator>();
builder.Services.AddScoped<IValidator<LogoutRequestDto>, LogoutRequestDtoValidator>();

builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RefreshTokenUseCase>();
builder.Services.AddScoped<LogoutUseCase>();
builder.Services.AddScoped<RegisterClientUseCase>();
builder.Services.AddScoped<GetCurrentUserProfileUseCase>();
builder.Services.AddScoped<CreateSalonOwnerUseCase>();
builder.Services.AddScoped<CreateProfessionalUseCase>();
builder.Services.AddScoped<CreateServiceUseCase>();
builder.Services.AddScoped<ListServicesUseCase>();
builder.Services.AddScoped<GetServiceByIdUseCase>();
builder.Services.AddScoped<UpdateServiceUseCase>();
builder.Services.AddScoped<DeleteServiceUseCase>();
builder.Services.AddScoped<CreateProfessionalAvailabilityUseCase>();
builder.Services.AddScoped<ListProfessionalAvailabilityUseCase>();
builder.Services.AddScoped<UpdateProfessionalAvailabilityUseCase>();
builder.Services.AddScoped<DeleteProfessionalAvailabilityUseCase>();

var app = builder.Build();

var skipMigrationsAndSeed = app.Configuration.GetValue<bool>("Database:SkipMigrationsAndSeed");
if (!skipMigrationsAndSeed)
{
    await app.MigrateAndSeedAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<AuthRateLimitContextMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AuthenticatedUserValidationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
