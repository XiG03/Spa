using Microsoft.AspNetCore.Mvc;
using SpaBookingWeb.Services.Client;
using SpaBookingWeb.ViewModels.Client;
using System;
using System.Threading.Tasks;

namespace SpaBookingWeb.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _bookingService.GetBookingPageDataAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(BookingSubmissionModel submission)
        {
            if (!ModelState.IsValid)
            {
                // Reload dữ liệu nếu lỗi validate
                var model = await _bookingService.GetBookingPageDataAsync();
                model.Submission = submission;
                return View(model);
            }

            try
            {
                var appointmentId = await _bookingService.CreateBookingAsync(submission);
                return RedirectToAction("Success", new { id = appointmentId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã có lỗi xảy ra: " + ex.Message);
                var model = await _bookingService.GetBookingPageDataAsync();
                model.Submission = submission;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Success(int id)
        {
            return View(id);
        }
    }
}