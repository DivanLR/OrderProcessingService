using Microsoft.AspNetCore.Mvc;

namespace OrderProcessing.Controllers
{
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
