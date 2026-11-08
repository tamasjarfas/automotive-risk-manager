// DTOs/AuthDtos.cs
// A bejelentkezéshez és a munkamenethez tartozó adatátviteli objektumok.

using System.ComponentModel.DataAnnotations;

namespace AutomotiveRiskManager.Api.DTOs;

/// <summary>Bejelentkezési kérés: felhasználónév VAGY e-mail-cím + jelszó.</summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "A felhasználónév vagy e-mail-cím megadása kötelező.")]
    [MaxLength(256)]
    public string Identifier { get; init; } = string.Empty;

    [Required(ErrorMessage = "A jelszó megadása kötelező.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>A bejelentkezett felhasználó adatai (jelszó vagy kivonat sosem szerepel benne).</summary>
public sealed class CurrentUserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
}

/// <summary>CSRF-védelem: az állapotmódosító kéréseknél az X-CSRF-TOKEN fejlécben kell visszaküldeni.</summary>
public sealed class CsrfTokenDto
{
    public string Token { get; init; } = string.Empty;
}

