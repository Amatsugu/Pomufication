using Google.Apis.YouTube.v3;

using Pomufication.Models;

namespace Pomufication.Services;

public class YouTubeService
{
	public YouTubeService()
	{

	}

	public async Task<ChannelInfo?> GetChannelInfoAsync(string channelId)
	{
		return null;
	}

	public async Task<List<ChannelInfo>> SearchChannelsAsync(string query)
	{
		throw new NotImplementedException();
	}
}

