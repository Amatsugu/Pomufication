using Microsoft.AspNetCore.Mvc;

using Pomu;

using Pomufication.Models;
using Pomufication.Services;

using System.Diagnostics;

namespace Pomufication.Controllers;
public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger, PomuService pomu)
	{
		_logger = logger;
	}

	public async Task<IActionResult> IndexAsync([FromServices] Pomufier pomu, [FromQuery] string url)
	{
		if (url == null)
			return View();
		var t = await pomu.LoadVideo(url);
		await pomu.DownloadVideo(url);
		return View("Index", t);
	}

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
