// DTOs/UserDtos.cs
// A felhasználókezeléshez (csak Administrator) tartozó adatátviteli objektumok.

using System.ComponentModel.DataAnnotations;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.DTOs;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsLockedOut { get; init; }
    public string Role { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }

    public static UserDto FromEntity(ApplicationUser u, string role) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        Email = u.Email ?? string.Empty,
        DisplayName = u.DisplayName,
        IsActive = u.IsActive,
        IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
        Role = role,
        CreatedAt = u.CreatedAt
    };
}

public sealed class CreateUserRequest
{
    [Required(ErrorMessage = "A felhasználónév kötelező.")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "A felhasználónév 3–64 karakter hosszú legyen.")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "A felhasználónév csak betűt, számot, '.', '_' és '-' karaktert tartalmazhat.")]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Az e-mail-cím kötelező.")]
    [EmailAddress(ErrorMessage = "Érvénytelen e-mail-cím.")]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "A megjelenített név kötelező.")]
    [MaxLength(150)]
    public string DisplayName { get; init; } = string.Empty;

    [Required(ErrorMessage = "A kezdeti jelszó megadása kötelező.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "A szerepkör megadása kötelező.")]
    public string Role { get; init; } = string.Empty;
}

public sealed class SetUserRoleRequest
{
    [Required(ErrorMessage = "A szerepkör megadása kötelező.")]
    public string Role { get; init; } = string.Empty;
}

public sealed class SetUserStatusRequest
{
    // bool? + Required: ha a mező hiányzik, hibát kapunk, nem pedig "false"-t (véletlen inaktiválást).
    [Required(ErrorMessage = "Az állapot megadása kötelező.")]
    public bool? IsActive { get; init; }
}

