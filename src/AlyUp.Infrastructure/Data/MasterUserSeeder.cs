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

        var masterAlreadyExists = await db.Users.AnyAsync(u => u.IsMaster);
        if (masterAlreadyExists)
        {
            return;
        }

        var existingByEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingByEmail is not null)
        {
            existingByEmail.IsMaster = true;
            existingByEmail.Role = UserRole.Admin;
            existingByEmail.IsActive = true;
            existingByEmail.Name = string.IsNullOrWhiteSpace(name) ? existingByEmail.Name : name;
            existingByEmail.PasswordHash = passwordHasher.Hash(password);
            existingByEmail.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            logger.LogInformation("Existing user promoted to master (Admin) by email: {Email}", email);
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(name) ? "Admin" : name,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsMaster = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("Master user (Admin) created with email: {Email}", email);
    }
}
