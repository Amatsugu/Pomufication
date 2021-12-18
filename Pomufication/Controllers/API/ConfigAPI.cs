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

	[HttpDelete("channel/{id}")]
	public IActionResult DeleteChannel(string id)
	{
		var cfg = _pomuService.Config;
		cfg.Channels.RemoveAll(c => c.ChannelId == id);
		_pomuService.SetConfig(cfg);
		return Ok();
	}

	[HttpPut("channel/{id}")]
	public IActionResult SaveChannel(string id, [FromForm] List<Keyword> config)
	{
		var cfg = _pomuService.Config;
		for (int i = 0; i < cfg.Channels.Count; i++)
		{
			if (cfg.Channels[i].ChannelId != id)
				continue;
			cfg.Channels[i].FilterKeywords = config;
		}
		_pomuService.SetConfig(_pomuService.Config);
		return Ok();
	}

	[HttpPost("channel/{id}/add")]
	public async Task<IActionResult> AddChannelAsync(string id)
	{
		var channel = await _pomuService.GetChannelAsync(id);
		if (channel == null)
			return Problem(title: $"The channel '{id}' could not be found", statusCode: StatusCodes.Status400BadRequest);
		var channelConfig = new ChannelConfig(id);
		var cfg = _pomuService.Config;
		if(cfg.Channels.Any(cfg => cfg.ChannelId == id))
			return Problem(title: $"The channel '{id}' already exists", statusCode: StatusCodes.Status400BadRequest);

		cfg.Channels.Add(channelConfig);
		_pomuService.SetConfig(cfg);
		return Ok();
	}

	[HttpGet("channel/search")]
	public async Task<IActionResult> SearchChannels([FromQuery] string query)
	{
		var results = await _pomuService.SearchChannels(query);
		return Ok(new
		{
			items = results
		});
	}
}