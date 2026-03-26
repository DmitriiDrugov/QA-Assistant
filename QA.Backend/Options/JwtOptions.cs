namespace QA.Backend.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "QA.Backend";
    public string Audience { get; set; } = "AURA.Frontend";
    public string Key { get; set; } = "development-only-key-change-this-before-production-2026";
    public int AccessTokenExpiresHours { get; set; } = 24;
}
