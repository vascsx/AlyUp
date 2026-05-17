namespace AlyUp.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }

    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    // duração em minutos
    public int DurationInMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();
}