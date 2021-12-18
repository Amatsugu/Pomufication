using YoutubeExplode.Channels;
using YoutubeExplode.Search;

namespace Pomufication.Models;

public class ChannelResultViewModel
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Url { get; set; }
	public string? Thumb { get; set; }
	public ChannelResultViewModel(ChannelSearchResult channel)
	{
		Id = channel.Id;
		Name = channel.Title;
		Url = channel.Url;
		Thumb = channel.Thumbnails.FirstOrDefault()?.Url;
	}
}
