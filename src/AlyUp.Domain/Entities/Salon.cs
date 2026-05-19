namespace AlyUp.Domain.Entities;

public class Salon
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = null;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<ProfessionalAvailability> ProfessionalAvailabilities { get; set; } = new List<ProfessionalAvailability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
