using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onboarding.Models;
using Microsoft.AspNetCore.Authorization;

namespace Onboarding.Controllers
{
    [Authorize] // Add this attribute to restrict access to authenticated users
    public class SignupController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public SignupController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                // Create the user with the provided email and password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Sign the user in after a successful registration
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect to a confirmation page or dashboard
                    return RedirectToAction("Confirmation");
                }

                // If user creation fails, add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If ModelState is not valid or registration fails, show validation errors
            return View(model);
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
