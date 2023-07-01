
using Flurl.Http;

using HtmlAgilityPack;
using Pomufication.Models.Youtube;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Pomufication.Services;

public class YouTubeService
{
	public YouTubeService()
	{

	}

	public string GetChannelUrl(string channelId)
	{
		return $"https://youtube.com/channel/{channelId}";
	}

	private Task<string> LoadChannelPageAsync(string id)
	{
		var url = GetChannelUrl(id);
		return url.GetStringAsync();
	}

	public async Task<ChannelInfo?> GetChannelInfoAsync(string channelId)
	{
		var html = await LoadChannelPageAsync(channelId);
		var info = ParseChannelInfo(html);
		return info;
	}
	
	public async Task<List<ChannelInfo>> SearchChannelsAsync(string query)
	{
		throw new NotImplementedException();
	}

	public async Task<List<VideoInfo>> GetUpcommingStreamsAsync(string channelId)
	{
		throw new NotImplementedException();
	}

	public async Task<VideoInfo> GetVideoAsync(string videoId)
	{
		throw new NotImplementedException();
	}

	public async Task<VideoInfo> GetVideoFromUrlAsync(string url)
	{
		throw new NotImplementedException();
	}

	private ChannelInfo? ParseChannelInfo(string html)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		var script = doc.DocumentNode.QuerySelectorAll("script")
			.First(a => a.InnerText.Contains("var ytInitialData"))
			.InnerText.Trim();
		var initData = script[20..^1];
		//File.WriteAllText("/test.json", initData);

		var json = JsonObject.Parse(initData);

		if (json == null)
			return null;

		var meta = json["metadata"]!["channelMetadataRenderer"]!;

		var iconUrl = meta["avatar"]!
			["thumbnails"]!
			[0]!
			["url"]!
			.GetValue<string>();

		var channelName = meta["title"]!.GetValue<string>();

		var channelId = meta["externalId"]!.GetValue<string>();
		var channelUrl = meta["channelUrl"]!.GetValue<string>();

		var channelInfo = new ChannelInfo(channelName, iconUrl, channelId, channelUrl);
		return channelInfo;
	}
}

