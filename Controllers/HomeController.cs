using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Onboarding.Models;

namespace Onboarding.Controllers
{
    public class HomeController : Controller
    {
        private readonly GraphApiService _graphService;

        public HomeController(GraphApiService graphService)
        {
            _graphService = graphService;
        }
        
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {   
                //Obtener del claims el extension_Roles y mostrarlo en la vista
                var roles = User.FindFirst("extension_Roles")?.Value;
                var email = User.FindFirst("emails")?.Value;
                ViewBag.Roles = roles;
                ViewBag.Email = email;
                
                //Filtrar el id del usuario desde el claims
                var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                var userInfo = await _graphService.GetUserById(userId, _graphService.GetAccessToken().Result);
                
                Console.WriteLine("BBBBBBbbbbbbbbbbbbbbbbb :"+ userInfo);
                Console.WriteLine("Ccccccccccccccccc :"+ email);
                
                if (userInfo.TryGetValue("mail", out var mail))
                    _graphService.SetUserMail(userId, email, _graphService.GetAccessToken().Result);
            }
            return View();
        }

        [Authorize(Policy = "Admins")]
        public IActionResult Claims()
        {
            if (User.Identity.IsAuthenticated)
            {   
                //Obtener del claims el extension_Roles y mostrarlo en la vista
                var roles = User.FindFirst("extension_Roles")?.Value;
                var email = User.FindFirst("emails")?.Value;
                ViewBag.Roles = roles;
                ViewBag.Email = email;
            }
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}