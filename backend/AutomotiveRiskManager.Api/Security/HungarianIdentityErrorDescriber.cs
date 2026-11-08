// Security/HungarianIdentityErrorDescriber.cs
// Magyar nyelvű hibaüzenetek az Identity leggyakoribb hibáihoz (jelszó-szabályok,
// duplikált felhasználónév/e-mail). A hibaüzenetek sosem tartalmazzák a jelszót.

using Microsoft.AspNetCore.Identity;

namespace AutomotiveRiskManager.Api.Security;

public class HungarianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Ismeretlen hiba történt." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Ez az e-mail-cím már foglalt." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Ez a felhasználónév már foglalt." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Érvénytelen e-mail-cím." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Érvénytelen felhasználónév." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"A jelszó legalább {length} karakter hosszú legyen." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "A jelszónak tartalmaznia kell legalább egy számjegyet." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "A jelszónak tartalmaznia kell legalább egy kisbetűt." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "A jelszónak tartalmaznia kell legalább egy nagybetűt." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"A jelszó legalább {uniqueChars} különböző karaktert tartalmazzon." };
}

