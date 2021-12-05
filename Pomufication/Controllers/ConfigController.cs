using Microsoft.AspNetCore.Mvc;

namespace Pomufication.Controllers;
public class ConfigController : Controller
{
	public IActionResult Index()
	{
		return View();
	}
}
