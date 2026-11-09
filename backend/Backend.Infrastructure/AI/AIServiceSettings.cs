namespace RepairShop.Infrastructure.AI;

public class AIServiceSettings
{
    public const string SectionName = "AIService";

    public string BaseUrl { get; set; } = default!;
    public int TimeoutSeconds { get; set; } = 30; // khá»›p Task 5 Tuáº§n 2: timeout = 5 giÃ¢y
    // Task 4.1: MAS cháº¡y 1 LLM call + dá»±ng context, cáº§n ngÃ¢n sÃ¡ch riÃªng (máº·c Ä‘á»‹nh 20s).
    public int ConsultTimeoutSeconds { get; set; } = 60;
}
