namespace Pomufication.Models.Youtube;

public record VideoInfo(string Id, string Title, string Url, ChannelInfo Channel, string? Description = null);
