namespace DocumentGenerator.Domain;

public sealed class TemplateRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string Category { get; set; } = "Інше";
    public string Sha256 { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
