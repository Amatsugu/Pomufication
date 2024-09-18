using Microsoft.VisualBasic;

namespace Pomufication.Models.Youtube;

public record VideoInfo(string Id, string Title, ChannelInfo Channel, DateTimeOffset StartTime)
{
	public string Url => $"https://youtube.com/watch?v={Id}";
};
