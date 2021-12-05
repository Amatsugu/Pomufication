using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Pomufication.Models;
using Pomufication.Services;

namespace Pomufication.Controllers.API;
[Route("api/config")]
[ApiController]
public class ConfigAPI : ControllerBase
{
	private readonly PomuService _pomuService;

	public ConfigAPI(PomuService pomuService)
	{
		_pomuService = pomuService;
	}


	[HttpPut("")]
	public IActionResult SaveConfig(PomuConfig config)
	{
		_pomuService.SetConfig(config);
		return Ok();
	}
}
