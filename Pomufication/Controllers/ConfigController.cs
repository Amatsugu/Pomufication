using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pomufication.Models;
using Pomufication.Services;

namespace Pomufication.Controllers;

[Route("config")]
[Authorize]
public class ConfigController : Controller
{
	private readonly ConfigService _configService;
	private readonly YouTubeService _youTubeService;

	public ConfigController(ConfigService configService, YouTubeService youTubeService)
	{
		_configService = configService;
		_youTubeService = youTubeService;
	}

	[HttpGet]
	public async Task<IActionResult> ConfigureAsync()
	{
		var viewModels = new List<ChannelViewModel>();
		for (int i = 0; i < _configService.Config.Channels.Count; i++)
		{
			var channelConfig = _configService.Config.Channels[i];
			var channel = await _youTubeService.GetChannelInfoAsync(channelConfig.ChannelId);
			viewModels.Add(new ChannelViewModel(channelConfig, channel));
		}

		return View("ConfigEditor", viewModels);
	}

	[HttpGet("system")]
	public IActionResult System()
	{
		return View("SystemConfig", _configService.Config);
	}

	[HttpGet("channel/{id}")]
	public async Task<IActionResult> ConfigureChannelAsync(string id)
	{
		var channelConfig = _configService.Config.Channels.FirstOrDefault(c => c.ChannelId == id);
		if(channelConfig == null)
			return NotFound();

		var channel = await _youTubeService.GetChannelInfoAsync(channelConfig.ChannelId);


		return View("EditChannel", new ChannelViewModel(channelConfig, channel)) ;
	}
}