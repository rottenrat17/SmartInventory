using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.Models;
using Serilog;

namespace SmartInventoryManagement.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            _logger.LogInformation("Home page accessed");
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View("Error");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var actualStatusCode = statusCode ?? HttpContext.Response.StatusCode;
        
        _logger.LogError("Error occurred. Status Code: {StatusCode}, Request ID: {RequestId}", 
            actualStatusCode, requestId);
        
        return View(new ErrorViewModel 
        { 
            RequestId = requestId,
            ShowRequestId = true,
            StatusCode = actualStatusCode,
            ErrorMessage = GetErrorMessageForStatusCode(actualStatusCode)
        });
    }
    
    private string GetErrorMessageForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            404 => "The requested resource could not be found.",
            401 => "Authentication is required to access this resource.",
            403 => "You do not have permission to access this resource.",
            400 => "The server could not understand the request due to invalid syntax or parameters.",
            500 => "The server encountered an unexpected condition that prevented it from fulfilling the request.",
            _ => "An error occurred while processing your request."
        };
    }
}
