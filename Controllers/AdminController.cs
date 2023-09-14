using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Onboarding.Controllers
{
    [Authorize(Policy = "Admins")]
    public class AdminController : Controller
    {
        private readonly GraphApiService _graphService;

        public AdminController(GraphApiService graphService)
        {
            _graphService = graphService;
        }

        public async Task<IActionResult> Index()
        {
            var token = await _graphService.GetAccessToken();
            var users = await _graphService.FetchUsers(token);
            return View(users);
        }

        public async Task<IActionResult> EditRole(string id)
        {
            var token = await _graphService.GetAccessToken();
            var user = await _graphService.GetUserById(id, token);

            if (user == null)
            {
                return View("Error");
            }

            var userEmail = user["mail"].ToString();
            var currentRole = user["rol"].ToString();

            ViewBag.UserId = id;
            ViewBag.UserEmail = userEmail;
            ViewBag.CurrentRole = currentRole;

            return View("EditRole");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(string UserId, string Role)
        {
            
            
            var token = await _graphService.GetAccessToken();
            
            
            var isUpdated = await _graphService.SetUserRole(UserId, Role, token);
            
            if (!isUpdated)
            {
                return View("Error");
            }
            
            
            return RedirectToAction("Index");
        }
    }
}