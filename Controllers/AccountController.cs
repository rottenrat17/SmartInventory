using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using SmartInventoryManagement.ViewModels;
using System;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Text.Encodings.Web;
using System.Linq;

namespace SmartInventoryManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password ?? string.Empty);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    
                    // Assign the User role to newly registered users
                    await _userManager.AddToRoleAsync(user, "User");
                    
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, Request.Scheme);

                    await _emailService.SendEmailConfirmationAsync(user.Email ?? string.Empty, confirmationLink ?? string.Empty);

                    return RedirectToAction("RegisterConfirmation", new { email = user.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email ?? string.Empty,
                    model.Password ?? string.Empty,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("ForgotPassword requested for email: {Email}", model.Email);
                    var user = await _userManager.FindByEmailAsync(model.Email ?? string.Empty);
                    if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        _logger.LogWarning("ForgotPassword: User not found or email not confirmed for {Email}", model.Email);
                        return RedirectToAction(nameof(ForgotPasswordConfirmation));
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    _logger.LogDebug("Generated password reset token for user {Email}", user.Email);

                    var callbackUrl = Url.Action("ResetPassword", "Account",
                        new { email = user.Email, token = token }, Request.Scheme);

                    _logger.LogDebug("Password reset callback URL: {URL}", callbackUrl);

                    if (string.IsNullOrEmpty(callbackUrl))
                    {
                        _logger.LogError("Failed to generate reset password URL");
                        ModelState.AddModelError(string.Empty, "Error generating password reset link.");
                        return View(model);
                    }

                    try
                    {
                        await _emailService.SendPasswordResetAsync(user.Email ?? string.Empty, callbackUrl);
                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending password reset email to {Email}", user.Email);
                        ModelState.AddModelError(string.Empty, "Error sending password reset email. Please try again later.");
                        return View(model);
                    }

                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in ForgotPassword for {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("ResetPassword GET: Missing email or token. Email: {EmailEmpty}, Token: {TokenEmpty}", 
                    string.IsNullOrEmpty(email), string.IsNullOrEmpty(token));
                return BadRequest("A code must be supplied for password reset.");
            }

            _logger.LogInformation("ResetPassword GET requested for email: {Email}", email);
            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation("ResetPassword POST requested for email: {Email}", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email ?? string.Empty);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    _logger.LogWarning("ResetPassword: User not found for {Email}", model.Email);
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                _logger.LogDebug("Attempting to reset password for user {Email}", user.Email);
                var result = await _userManager.ResetPasswordAsync(user, model.Token ?? string.Empty, model.Password ?? string.Empty);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user {Email}", user.Email);
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}", 
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ResetPassword for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            
            var model = new ManageViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Username = user.UserName,
                DateJoined = user.DateJoined,
                EmailConfirmed = user.EmailConfirmed
            };
            
            return View(model);
        }
        
        [HttpPost]
        public async Task<IActionResult> Manage(ManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Your profile has been updated";
                return RedirectToAction(nameof(Manage));
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return View("ResendEmailConfirmation");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return RedirectToAction("Login");
            }

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Create confirmation link
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            // Send confirmation email
            if (callbackUrl != null && user.Email != null)
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, callbackUrl);
            }
            else
            {
                _logger.LogError("Failed to generate confirmation URL or retrieve email for user {UserId}", user.Id);
            }

            // Redirect to confirmation sent page
            ViewBag.Email = email;
            return View("ResendEmailConfirmation");
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult LoginWith2fa(string returnUrl)
        {
            return RedirectToAction(nameof(LoginWith2fa), new { ReturnUrl = returnUrl });
        }

        private IActionResult Lockout()
        {
            return View("Lockout");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, 
                model.CurrentPassword ?? string.Empty, 
                model.NewPassword ?? string.Empty);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User changed their password successfully.");
                await _signInManager.RefreshSignInAsync(user);
                TempData["StatusMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Manage));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new TwoFactorAuthenticationViewModel
            {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult DeleteAccount()
        {
            return View(new DeleteAccountViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeleteAccount(DeleteAccountViewModel model)
        {
            // If manual validation needed, check the confirmDelete flag
            if (!model.ConfirmDelete)
            {
                ModelState.AddModelError("ConfirmDelete", "You must confirm that you want to delete your account");
            }

            if (!ModelState.IsValid)
            {
                return View("DeleteAccount", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password ?? string.Empty);
            if (!isPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return View("DeleteAccount", model);
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User deleted their account.");
                await _signInManager.SignOutAsync();
                TempData["StatusMessage"] = "Your account has been permanently deleted.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("DeleteAccount", model);
        }
    }
} 