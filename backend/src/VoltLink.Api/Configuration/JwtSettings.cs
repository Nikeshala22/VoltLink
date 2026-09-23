namespace VoltLink.Api.Configuration;


public class JwtSettings
{

    public const string SectionName = "Jwt";

  
    public string Key { get; set; } = string.Empty;


    public string Issuer { get; set; } = "VoltLink.Api";

    public string Audience { get; set; } = "VoltLink.Clients";


    public int ExpiryMinutes { get; set; } = 480;
}
