namespace AlyUp.Domain.Entities;

public class Client
{
    public Guid Id { get; set; }

    // Relacionamento com o SaaS
    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    // Dados do cliente
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Relacionamento
    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();
}
