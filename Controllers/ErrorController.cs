using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode
            };

            switch (statusCode)
            {
                case 404:
                    _logger.LogWarning($"404 Error: Page not found - {Request.Path}");
                    errorViewModel.ErrorMessage = "Sorry, the page you are looking for could not be found.";
                    return View("NotFound", errorViewModel);
                case 500:
                    _logger.LogError($"500 Error: Internal server error - {Request.Path}");
                    errorViewModel.ErrorMessage = "Sorry, something went wrong on our end. Please try again later.";
                    return View("InternalServerError", errorViewModel);
                case 403:
                    _logger.LogWarning($"403 Error: Access denied - {Request.Path}");
                    errorViewModel.ErrorMessage = "Sorry, you don't have permission to access this resource.";
                    return View("AccessDenied", errorViewModel);
                default:
                    _logger.LogError($"Unexpected error: {statusCode} - {Request.Path}");
                    errorViewModel.ErrorMessage = "An unexpected error occurred. Please try again later.";
                    return View("Error", errorViewModel);
            }
        }

        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = "An error occurred while processing your request."
            };

            _logger.LogError($"Unhandled error: {errorViewModel.RequestId}");

            return View(errorViewModel);
        }
    }
} 