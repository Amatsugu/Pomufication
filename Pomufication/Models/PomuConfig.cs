using System.Text.RegularExpressions;

namespace Pomufication.Models;

public class PomuConfig
{
	public List<ChannelConfig> Channels { get; set; }

	public PomuConfig()
	{
		Channels = new List<ChannelConfig>();
	}

	public PomuConfig(List<ChannelConfig> channels)
	{
		Channels = channels;
	}
}

public class ChannelConfig
{
	public string ChannelId { get; set; }
	public bool Enabled { get; set; }
	public List<Keyword> FilterKeywords { get; set; }

	public ChannelConfig()
	{
		ChannelId = string.Empty;
		Enabled = false;
		FilterKeywords = new List<Keyword>();
	}

	public ChannelConfig(string channelId, List<Keyword>? keywords = null)
	{
		ChannelId = channelId;
		Enabled = true;
		FilterKeywords = keywords ?? new List<Keyword>();
	}
}

public class Keyword
{
	public string[] Filters { get; set; }
	public bool Enabled { get; set; }
	public StringComparison Comparison { get; set; } = StringComparison.OrdinalIgnoreCase;
	public RegexOptions RegexOptions { get; set; }
	public KeywordType Type { get; set; }

	public Keyword()
	{
		Filters = Array.Empty<string>();
	}

	public Keyword(string[] filters)
	{
		Filters = filters;
	}

	public bool Match(string value)
	{
		Console.WriteLine($"Checking '{value}' against '{string.Join(",", Filters)}'");
		if (!Enabled)
			return true;
		return Type switch
		{
			KeywordType.Word => Filters.Any(f => value.Contains(f, Comparison)),
			KeywordType.Regex => Filters.Any(f => Regex.Match(value, f, RegexOptions).Success),
			_ => false
		};
	}
}

public enum KeywordType
{
	Word,
	Regex
}