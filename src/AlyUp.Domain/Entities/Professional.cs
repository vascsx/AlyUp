namespace AlyUp.Domain.Entities;

public class Professional : ISalonScopedEntity
{
    public Guid Id { get; set; }

    public User User { get; set; } = null!;

    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = null;

    public ICollection<ProfessionalAvailability> ProfessionalAvailabilities { get; set; }
        = new List<ProfessionalAvailability>();
}
