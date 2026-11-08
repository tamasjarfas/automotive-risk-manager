// Models/ApplicationUser.cs
// Az alkalmazás felhasználója. Az ASP.NET Core Identity IdentityUser<Guid> osztályát
// bővíti (a Supplier-hez hasonlóan Guid azonosítóval).
//
// Az IdentityUser már tartalmazza: UserName, Email, PasswordHash (biztonságos kivonat,
// a jelszó sosem tárolódik nyílt szövegként), SecurityStamp, LockoutEnd, AccessFailedCount.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AutomotiveRiskManager.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>A felhasználó megjelenített neve (pl. a fejlécben).</summary>
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Inaktív felhasználó nem tud bejelentkezni, a meglévő munkamenete megszűnik.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>A fiók létrehozásának ideje (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

