namespace Hotel.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Хеш від PasswordHasher. Пароль у відкритому вигляді не зберігається.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
