namespace Order.Infrastructure.ExternalServices;

public sealed class CatalogClientOptions
{
    public const string SectionName = "Services";

    public string CatalogApiBaseUrl { get; set; } = string.Empty;
}
