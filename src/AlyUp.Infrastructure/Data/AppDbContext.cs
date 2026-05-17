using AlyUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureClient(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureSalon(modelBuilder);
        ConfigureService(modelBuilder);
        ConfigureAppointment(modelBuilder);
        ConfigureRefreshToken(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureClient(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Client>()
            .ToTable("clients")
            .HasKey(c => c.Id);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Phone)
            .HasMaxLength(20);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Email)
            .HasMaxLength(150);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Notes)
            .HasMaxLength(500);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.CreatedAt)
            .IsRequired();

        modelBuilder
            .Entity<Client>()
            .HasOne(c => c.Salon)
            .WithMany(s => s.Clients)
            .HasForeignKey(c => c.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<User>();
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(150);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(u => u.Salon)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SalonId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSalon(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Salon>();
        builder.ToTable("salons");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Document).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => s.Document).IsUnique();
        builder.Property(s => s.Address).IsRequired().HasMaxLength(250);
    }

    private static void ConfigureService(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Service>();
        builder.ToTable("services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Price).IsRequired();
        builder.Property(s => s.DurationInMinutes).IsRequired();

        builder.HasOne(s => s.Salon)
            .WithMany(salon => salon.Services)
            .HasForeignKey(s => s.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAppointment(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Appointment>();
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.StartDateTime).IsRequired();
        builder.Property(a => a.EndDateTime).IsRequired();
        builder.Property(a => a.Price).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasOne(a => a.Salon)
            .WithMany(salon => salon.Appointments)
            .HasForeignKey(a => a.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<RefreshToken>();
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Token).IsRequired();
        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.Property(rt => rt.Created).IsRequired();
        builder.Property(rt => rt.Expires).IsRequired();
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);
    }
}
