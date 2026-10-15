// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext context,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _emailSender = emailSender;
    }

    // Registration form properties
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        [RegularExpression("^(Organiser|Participant)$",
            ErrorMessage = "Select Organiser or Participant.")]
        [Display(Name = "Requested Role")]
        public string RequestedRole { get; set; } = default!;
    }

    // Display registration page
    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        ExternalLogins = (await _signInManager
            .GetExternalAuthenticationSchemesAsync()).ToList();
    }

    // Process registration
    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Content("~/");
        }

        ReturnUrl = returnUrl;

        ExternalLogins = (await _signInManager
            .GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.RequestedRole != "Organiser" &&
            Input.RequestedRole != "Participant")
        {
            ModelState.AddModelError(
                "Input.RequestedRole",
                "Please select a valid role.");

            return Page();
        }

        var user = CreateUser();

        await _userStore.SetUserNameAsync(
            user, Input.Email, CancellationToken.None);

        await _emailStore.SetEmailAsync(
            user, Input.Email, CancellationToken.None);

        // Create the user and pending request together
        await using (var transaction =
            await _context.Database.BeginTransactionAsync())
        {
            var result = await _userManager.CreateAsync(
                user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return Page();
            }

            var roleRequest = new RoleRequest
            {
                UserId = user.Id,
                RequestedRole = Input.RequestedRole,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.RoleRequests.Add(roleRequest);

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Failed to save role request.");

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to complete registration. Please try again.");

                return Page();
            }
        }

        _logger.LogInformation(
            "User registered and requested {Role} access.",
            Input.RequestedRole);

        // Generate email confirmation
        var userId = await _userManager.GetUserIdAsync(user);

        var code = await _userManager
            .GenerateEmailConfirmationTokenAsync(user);

        code = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(code));

        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new
            {
                area = "Identity",
                userId,
                code,
                returnUrl
            },
            protocol: Request.Scheme)!;

        await _emailSender.SendEmailAsync(
            Input.Email,
            "Confirm your email",
            $"Please confirm your account by " +
            $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
            $"clicking here</a>.");

        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage(
                "RegisterConfirmation",
                new { email = Input.Email, returnUrl });
        }

        await _signInManager.SignInAsync(
            user, isPersistent: false);

        return LocalRedirect(returnUrl);
    }

    // Create Identity user
    private IdentityUser CreateUser()
    {
        return new IdentityUser();
    }

    // Get email store
    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException(
                "The user store must support email.");
        }

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}
