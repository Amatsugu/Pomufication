using Microsoft.AspNetCore.Mvc;

using Pomufication.Models;
using Pomufication.Services;

namespace Pomufication.Controllers;

[Route("config")]
public class ConfigController : Controller
{
	private readonly PomuService _pomuService;

	public ConfigController(PomuService pomuService)
	{
		_pomuService = pomuService;
	}

	[HttpGet]
	public async Task<IActionResult> ConfigureAsync()
	{
		var viewModels = new List<ChannelViewModel>();
		for (int i = 0; i < _pomuService.Config.Channels.Count; i++)
		{
			var channelConfig = _pomuService.Config.Channels[i];
			var channel = await _pomuService.GetChannelAsync(channelConfig.ChannelId);
			viewModels.Add(new ChannelViewModel(channelConfig, channel));
		}

		return View("ConfigEditor", viewModels);
	}

	[HttpGet("system")]
	public IActionResult System()
	{
		return View("SystemConfig", _pomuService.Config);
	}

	[HttpGet("channel/{id}")]
	public async Task<IActionResult> ConfigureChannelAsync(string id)
	{
		var channelConfig = _pomuService.Config.Channels.FirstOrDefault(c => c.ChannelId == id);
		if(channelConfig == null)
			return NotFound();
		
		var channel = await _pomuService.GetChannelAsync(channelConfig.ChannelId);


		return View("EditChannel", new ChannelViewModel(channelConfig, channel)) ;
	}
}