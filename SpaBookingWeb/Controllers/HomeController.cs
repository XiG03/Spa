using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using SpaBookingWeb.Models;
using SpaBookingWeb.Services.Client;

namespace SpaBookingWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IClientHomeService _clientHomeService;

        public HomeController(ILogger<HomeController> logger, IClientHomeService clientHomeService)
        {
            _logger = logger;
            _clientHomeService = clientHomeService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> HomeClient()
        {
            var model = await _clientHomeService.GetHomeDataAsync();
            return View(model);
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
