using System.ComponentModel.DataAnnotations;

namespace AlyUp.Domain.Entities;
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; }
        public DateTime? Revoked { get; set; }
        public bool IsActive => Revoked == null && !IsExpired;
    }
