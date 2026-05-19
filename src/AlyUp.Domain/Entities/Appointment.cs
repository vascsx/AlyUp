using AlyUp.Domain.Enums;

namespace AlyUp.Domain.Entities;

public class Appointment : ISalonScopedEntity
{
    public Guid Id { get; set; }

    // Multi-tenant
    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;

    // Cliente
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    // Serviço
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // Horário
    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    // Valor salvo no momento do atendimento
    public decimal Price { get; set; }

    // Status
    public AppointmentStatus Status { get; set; }

    // Observações
    public string? Notes { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}