using Microsoft.AspNetCore.Mvc;

namespace OrderProcessing.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
