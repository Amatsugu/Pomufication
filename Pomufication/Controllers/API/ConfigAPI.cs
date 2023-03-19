using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Pomufication.Models;
using Pomufication.Services;

namespace Pomufication.Controllers.API;

[Route("api/config")]
[ApiController]
[Authorize]
public class ConfigAPI : ControllerBase
{
	private readonly ConfigService _configService;
	private readonly YouTubeService _youTubeService;

	public ConfigAPI(ConfigService configService, YouTubeService youTubeService)
	{
		_configService = configService;
		_youTubeService = youTubeService;
	}

	[HttpDelete("channel/{id}")]
	public IActionResult DeleteChannel(string id)
	{
		var cfg = _configService.Config;
		cfg.Channels.RemoveAll(c => c.ChannelId == id);
		_configService.SetConfig(cfg);
		return Ok();
	}

	[HttpPut("channel/{id}")]
	public IActionResult SaveChannel(string id, [FromForm] List<Keyword> config)
	{
		var cfg = _configService.Config;
		for (int i = 0; i < cfg.Channels.Count; i++)
		{
			if (cfg.Channels[i].ChannelId != id)
				continue;
			cfg.Channels[i].FilterKeywords = config;
		}
		_configService.SetConfig(_configService.Config);
		return Ok();
	}

	[HttpPost("channel/{id}/add")]
	public async Task<IActionResult> AddChannelAsync(string id)
	{
		var channel = await _youTubeService.GetChannelInfoAsync(id);
		if (channel == null)
			return Problem(title: $"The channel '{id}' could not be found", statusCode: StatusCodes.Status400BadRequest);
		var channelConfig = new ChannelConfig(id);
		var cfg = _configService.Config;
		if(cfg.Channels.Any(cfg => cfg.ChannelId == id))
			return Problem(title: $"The channel '{id}' already exists", statusCode: StatusCodes.Status400BadRequest);

		cfg.Channels.Add(channelConfig);
		_configService.SetConfig(cfg);
		return Ok();
	}

	[HttpGet("channel/search")]
	public async Task<IActionResult> SearchChannels([FromQuery] string query)
	{
		var results = await _youTubeService.SearchChannelsAsync(query);
		return Ok(new
		{
			items = results
		});
	}

	[HttpPut("system")]
	public IActionResult UpdateSystem([FromForm] PomuConfig config)
	{
		var cfg = _configService.Config;
		cfg.DataDirectory = config.DataDirectory;
		_configService.SetConfig(cfg);
		return Ok();
	}
}