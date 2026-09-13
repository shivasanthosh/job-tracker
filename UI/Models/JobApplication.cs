namespace UI.Models;

public class JobApplication
{
    public string? Id { get; set; }
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string JobLink { get; set; } = "";
    public string Status { get; set; } = "Applied";
    public string DateApplied { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string LastUpdate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string Notes { get; set; } = "";
}

public class AppUser
{
    public string Uid { get; set; } = "";
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
}

public static class ApplicationStatus
{
    public static readonly string[] All =
    {
        "Applied", "OA", "Interviewing", "Offer", "Rejected", "Withdrawn"
    };
}
