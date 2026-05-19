namespace AlyUp.Domain.Entities;

public class Service : ISalonScopedEntity
{
    public Guid Id { get; set; }

    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationInMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();
}
