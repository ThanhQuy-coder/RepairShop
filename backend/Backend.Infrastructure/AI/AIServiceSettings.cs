namespace RepairShop.Infrastructure.AI;

public class AIServiceSettings
{
    public const string SectionName = "AIService";

    public string BaseUrl { get; set; } = default!;
    public int TimeoutSeconds { get; set; } = 30; // khớp Task 5 Tuần 2: timeout = 5 giây
}