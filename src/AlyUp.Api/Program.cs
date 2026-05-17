using AlyUp.Api.Filters;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Admin;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Application.UseCases.Salon;
using AlyUp.Application.Validators;
using AlyUp.Infrastructure.Data;
using AlyUp.Infrastructure.Extensions;
using AlyUp.Infrastructure.Middleware;
using AlyUp.Infrastructure.Repositories;
using AlyUp.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAppAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISalonRepository, SalonRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGeneratorService>();
builder.Services.AddScoped<IInputNormalizer, InputNormalizer>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<ValidationActionFilter>();

builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterClientRequestDto>, RegisterClientRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateSalonOwnerRequestDto>, CreateSalonOwnerRequestDtoValidator>();
builder.Services.AddScoped<IValidator<CreateProfessionalRequestDto>, CreateProfessionalRequestDtoValidator>();

builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RegisterClientUseCase>();
builder.Services.AddScoped<CreateSalonOwnerUseCase>();
builder.Services.AddScoped<CreateProfessionalUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.MigrateAndSeedAsync();

app.Run();
