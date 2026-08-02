using Microsoft.AspNetCore.Mvc;

namespace OrderProcessing.Controllers;

public class OrderController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
