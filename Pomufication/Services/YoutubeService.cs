
using Flurl.Http;

using HtmlAgilityPack;

using Microsoft.CodeAnalysis.CSharp.Syntax;

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

	public string GetChannelUrlFromUsername(string username)
	{
		return $"https://youtube.com/{username}";
	}

	private Task<string> LoadChannelPageAsync(string id)
	{
		var url = GetChannelUrl(id);
		return url.GetStringAsync();
	}

	public async Task<ChannelInfo?> GetChannelInfoAsync(string channelId)
	{
		var html = await LoadChannelPageAsync(channelId);
		var json = ParseChannelData(html);
		if(json == null)
			return null;
		var info = ReadChannelInfo(json);
		return info;
	}
	
	public async Task<List<ChannelInfo>> SearchChannelsAsync(string query)
	{
		throw new NotImplementedException();
	}

	public async Task<List<VideoInfo>> GetUpcomingStreamsAsync(string channelId)
	{
		var html = await LoadChannelPageAsync(channelId);
		var json = ParseChannelData(html);
		if (json == null)
			return new List<VideoInfo>(0);
		return ReadUpcomingStreamsAsync(json);
	}

	public async Task<VideoInfo> GetVideoAsync(string videoId)
	{
		throw new NotImplementedException();
	}

	public async Task<VideoInfo> GetVideoFromUrlAsync(string url)
	{
		throw new NotImplementedException();
	}

	private JsonNode? ParseChannelData(string html)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		var script = doc.DocumentNode.QuerySelectorAll("script")
			.First(a => a.InnerText.Contains("var ytInitialData"))
			.InnerText.Trim();
		var initData = script[20..^1];
#if DEBUG
		//File.WriteAllText("/test.json", initData);
#endif

		var json = JsonNode.Parse(initData);

		if (json == null)
			return null;

		return json;
	}

	private List<VideoInfo> ReadUpcomingStreamsAsync(JsonNode json, bool includeLiveNow = true)
	{

		var tabs = json["contents"]!["twoColumnBrowseResultsRenderer"]!["tabs"]!.AsArray();

		var homeTab = tabs.First(t => t!["tabRenderer"]!["title"]!.GetValue<string>() == "Home")!["tabRenderer"]!;

		var contentSections = homeTab["content"]!
									["sectionListRenderer"]!
									["contents"]!.AsArray();

		var allShelfs = contentSections.SelectMany(s => s!["itemSectionRenderer"]!["contents"]!
						.AsArray().Select(c => c!));

		var featuredContent = allShelfs.FirstOrDefault(s => s["channelFeaturedContentRenderer"] != null);

		var otherShelfs = allShelfs.Where(s => s["shelfRenderer"] != null).Select(s => s["shelfRenderer"]!);

		var upcomingShelf = otherShelfs.FirstOrDefault(s => 
			s!["title"]!["runs"]!.AsArray()
			.Any(t => t!["text"]!.GetValue<string>() == "Upcoming live streams")
		);

		if (upcomingShelf == null)
			return new List<VideoInfo>(0);

		var upcomingContent = upcomingShelf!["content"]!;
		var upcomingItems = upcomingContent["horizontalListRenderer"]?["items"]!.AsArray();
		var isExpanded = false;
		if (upcomingItems == null)
		{
			upcomingItems = upcomingContent["expandedShelfContentsRenderer"]!["items"]!.AsArray();
			isExpanded = true;
		}

		var results = new List<VideoInfo>(upcomingItems.Count);

		var channelInfo = ReadChannelInfo(json);

		foreach (var item in upcomingItems)
		{
			var videoItem = isExpanded ? item!["videoRenderer"]! : item!["gridVideoRenderer"]!;
			var videoId = videoItem["videoId"]!.GetValue<string>();
			var title = videoItem["title"]!["simpleText"]!.GetValue<string>();
			var startTimeInfo = videoItem["upcomingEventData"]!["startTime"]!.GetValue<string>();
			if(int.TryParse(startTimeInfo, out var timeInt))
			{
				var start = DateTimeOffset.UnixEpoch.AddSeconds(timeInt).LocalDateTime;
			}

			results.Add(new VideoInfo(videoId, title, channelInfo));
		}

		return results;
	}

	private ChannelInfo ReadChannelInfo(JsonNode json)
	{
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

