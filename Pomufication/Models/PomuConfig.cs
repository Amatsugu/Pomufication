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
	public string Filter { get; set; }
	public bool Enabled { get; set; }
	public StringComparison Comparison { get; set; } = StringComparison.OrdinalIgnoreCase;
	public RegexOptions RegexOptions { get; set; }
	public KeywordType Type { get; set; }

	public Keyword()
	{
		Filter = string.Empty;
	}

	public Keyword(string filter)
	{
		Filter = filter;
	}

	public bool Match(string value)
	{
		Console.WriteLine($"Checking '{value}' against '{Filter}'");
		if (!Enabled)
			return true;
		return Type switch
		{
			KeywordType.Word => value.Contains(Filter, Comparison),
			KeywordType.Regex => Regex.Match(value, Filter, RegexOptions).Success,
			_ => false
		};
	}
}

public enum KeywordType
{
	Word,
	Regex
}