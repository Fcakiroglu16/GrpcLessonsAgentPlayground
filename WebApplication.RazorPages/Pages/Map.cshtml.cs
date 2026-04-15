using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication.RazorPages.Pages;

public class MapModel : PageModel
{
    private readonly IConfiguration _configuration;

    public string NotificationServiceUrl { get; set; } = string.Empty;

    public MapModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        NotificationServiceUrl = _configuration["services:notificationservice:https:0"]
            ?? _configuration["services:notificationservice:http:0"]
            ?? "https://localhost:5003";
    }
}
