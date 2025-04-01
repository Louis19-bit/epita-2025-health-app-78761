using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public string Role { get; set; } // "Patient", "Doctor", "Admin"
    public string? Specialization { get; set; } // Nullable to prevent SQLite errors
    public bool ReceiveEmailNotifications { get; set; } // Ajout de la propriété manquante
}