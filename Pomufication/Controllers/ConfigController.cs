using Microsoft.AspNetCore.Mvc;

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
		var channelLookup = new Dictionary<string, YoutubeExplode.Channels.Channel>();

		for (int i = 0; i < _pomuService.Config.Channels.Count; i++)
		{
			var channel = _pomuService.Config.Channels[i];
			var c = await _pomuService.GetChannelAsync(channel.ChannelId);
			if (c == null)
				continue;
			channelLookup.Add(channel.ChannelId, c);
		}

		ViewData["lookup"] = channelLookup;

		return View("ConfigEditor", _pomuService.Config);
	}
}