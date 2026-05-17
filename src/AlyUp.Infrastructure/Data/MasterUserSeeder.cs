using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlyUp.Infrastructure.Data;

public static class MasterUserSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IInputNormalizer inputNormalizer,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["MasterUser:Email"];
        var password = configuration["MasterUser:Password"];
        var name = configuration["MasterUser:Name"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "MasterUser seed skipped because MasterUser:Email and/or MasterUser:Password is not configured.");
            return;
        }

        var normalizedEmail = inputNormalizer.NormalizeEmail(email);
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "Master" : inputNormalizer.NormalizeText(name);

        var masterAlreadyExists = await db.Users.AnyAsync(u => u.Role == UserRole.Master);
        if (masterAlreadyExists)
        {
            return;
        }

        var existingByEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (existingByEmail is not null)
        {
            existingByEmail.Role = UserRole.Master;
            existingByEmail.IsActive = true;
            existingByEmail.Name = normalizedName;
            existingByEmail.PasswordHash = passwordHasher.Hash(password);
            existingByEmail.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            logger.LogInformation("Existing user promoted to master by email: {Email}", normalizedEmail);
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.Master,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("Master user created with email: {Email}", normalizedEmail);
    }
}
