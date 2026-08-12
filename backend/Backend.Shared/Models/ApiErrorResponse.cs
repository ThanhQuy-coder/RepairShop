namespace Backend.Shared.Models;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = default!;
    public List<string> Errors { get; set; } = new();
}