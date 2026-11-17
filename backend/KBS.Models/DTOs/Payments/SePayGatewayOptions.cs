namespace KBS.Models.DTOs.Payments;

public class SePayGatewayOptions
{
    public bool Enabled { get; init; }
    public string Environment { get; init; } = "sandbox";
    public string MerchantId { get; init; } = "";
    public string SecretKey { get; init; } = "";
    public string IpnSecret { get; init; } = "";
    public string PublicBaseUrl { get; init; } = "";
    public string FrontendUrl { get; init; } = "http://localhost:5173";
    public bool Ready => Enabled && Environment == "sandbox" && MerchantId.StartsWith("SP-TEST-")
        && SecretKey.StartsWith("spsk_test_") && IpnSecret.Length >= 32
        && Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var url) && url.Scheme == "https";
}

public record GatewayCheckoutDto(string Url, Dictionary<string, string> Fields, string Environment);
