using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pomu;

using Pomufication.Models;
using Pomufication.Services;

using System.Diagnostics;

namespace Pomufication.Controllers;
public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger)
	{
		_logger = logger;
	}

	//[HttpGet]
	//[Authorize]
	//public async Task<IActionResult> IndexAsync([FromServices] Pomufier pomu, [FromQuery] string url)
	//{
	//	if (url == null)
	//		return View();
	//	var t = await pomu.LoadVideo(url);
	//	await pomu.DownloadVideo(url);
	//	return View("Index", t);
	//}

	[HttpGet("login")]
	public IActionResult Login([FromQuery] string? returnUrl, [FromQuery] string? code, [FromServices] AuthService authService)
	{
		if (code == null)
			goto Login;
		var token = authService.ExchangeCode(code);
		if (token == null)
			goto Login;
		Response.Cookies.Append("token", token);
		return LocalRedirect(returnUrl ?? "/");



		Login:
		Console.WriteLine($"Login Code: {authService.GetLoginCode()}");
		return View();

	}


	[HttpGet("error")]
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
