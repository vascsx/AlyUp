namespace AlyUp.Domain.Entities;

public class ProfessionalAvailability : ISalonScopedEntity
{
    public Guid Id { get; set; }

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
