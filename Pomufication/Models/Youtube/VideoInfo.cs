namespace Pomufication.Models.Youtube;

public record VideoInfo(string Id, string Title, ChannelInfo Channel)
{
	public string Url => $"https://youtube.com/watch?=${Id}";
};
