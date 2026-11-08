// Models/ApplicationRole.cs
// Szerepkör (Administrator, RiskManager, Viewer). Az Identity IdentityRole<Guid> osztályára épül.

using Microsoft.AspNetCore.Identity;

namespace AutomotiveRiskManager.Api.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
        Id = Guid.NewGuid();
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
    }
}

