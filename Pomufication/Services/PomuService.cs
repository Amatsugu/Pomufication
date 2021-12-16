using Pomu;

using Pomufication.Models;

using System.Diagnostics;
using System.Text.Json;

using YoutubeExplode;

namespace Pomufication.Services;

public class PomuService : IDisposable
{
	public PomuConfig Config { get; private set; }
	private readonly Pomufier _pomufier;
	private readonly ILogger<PomuService> _logger;
	private readonly YoutubeClient _youtube;

	private List<ActiveDownload> _activeDownloads;

	private Timer _timer;

	private bool _isSyncing;

	public PomuService(Pomufier pomufier, ILogger<PomuService> logger)
	{
		_pomufier = pomufier;
		_logger = logger;
		_youtube = pomufier.YoutubeClient;
		_activeDownloads = new List<ActiveDownload>();
		Config = LoadConfig();
		_timer = StartTimer();
	}

	private record ActiveDownload(string Url, Process Process);

	private Timer StartTimer()
	{
		return new Timer(Sync, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
	}

	internal ValueTask<YoutubeExplode.Channels.Channel> GetChannelAsync(string channelId)
	{
		return _youtube.Channels.GetAsync(channelId);
	}

	private async Task<Process> StartStreamlinkAsync(string url)
	{
		var video = await _youtube.Videos.GetAsync(url);

		var fileName = CleanTitle(video);

		var processInfo = new ProcessStartInfo
		{
			FileName = "streamlink",
			Arguments = $"{url} best --retry-streams 30 -o {fileName}.mp4 {GetCoookieString()}",
		};
		var p = Process.Start(processInfo);
		if (p == null)
			throw new Exception("Failed to start streamlink");
		return p;
	}

	private string? GetCoookieString()
	{
		if (!File.Exists("cookies.txt"))
			return null;
		//TODO: Generate cookie string
		return null;
	}

	private string CleanTitle(YoutubeExplode.Videos.Video video)
	{
		return $"'{video.Author.Title}_{video.Id}'";
	}

	public void SetConfig(PomuConfig config)
	{
		Config = config;
		SaveConfig();
	}

	private async void Sync(object? state)
	{
		if (_isSyncing)
			return;
		try
		{
			_isSyncing = true;
			_logger.LogInformation("Starting Sync");
			var streams = await CheckForNewStreams();
			for (int i = 0; i < streams.Count; i++)
			{
				var url = streams[i];
				try
				{
					if (_activeDownloads.Any(d => d.Url == url))
						continue;
					var download = await StartStreamlinkAsync(url);
					_activeDownloads.Add(new ActiveDownload(url, download));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to donwload stream. {url}", url);
				}
			}

			for (int i = 0; i < _activeDownloads.Count; i++)
			{
				var download = _activeDownloads[i];
				if (download.Process.HasExited)
					_activeDownloads.RemoveAt(i--);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Syncing Failed");
		}
		finally
		{
			_isSyncing = false;
		}
	}

	public async Task<List<string>> CheckForNewStreams()
	{
		var foundStreams = new List<string>();

		for (int i = 0; i < Config.Channels.Count; i++)
		{
			var channelConfig = Config.Channels[i];
			if (!channelConfig.Enabled)
				continue;
			var channel = await _youtube.Channels.GetAsync(channelConfig.ChannelId);
			if (channel == null)
			{
				_logger.LogWarning("Could not find a channel with id '{Id}'. Skipping...", channelConfig.ChannelId);
				continue;
			}
			_logger.LogInformation($"Checking for streams: {channel.Title}");
			var upcomingStreams = await _youtube.Search.GetVideosAsync(channel.Title)
				.Where(v => v.Author.ChannelId == channel.Id && v.Duration == null)
				.ToListAsync();
			var matchingStreams = upcomingStreams.Where(v => channelConfig.FilterKeywords.All(k => k.Match(v.Title)))
				.ToList();
			if (matchingStreams.Any())
				foundStreams.AddRange(matchingStreams.Select(v => v.Url));
			await Task.Delay(100);
		}

		return foundStreams;
	}

	public void SaveConfig()
	{
		var cfgJson = JsonSerializer.Serialize(Config);
		File.WriteAllText("config.json", cfgJson);
	}

	private static PomuConfig LoadConfig()
	{
		if (File.Exists("config.json"))
		{
			var configData = File.ReadAllText("config.json");
			return JsonSerializer.Deserialize<PomuConfig>(configData) ?? new PomuConfig();
		}
		else
		{
			var cfg = new PomuConfig();
			var cfgJson = JsonSerializer.Serialize(cfg);
			File.WriteAllText("config.json", cfgJson);
			return cfg;
		}
	}

	public void Dispose()
	{
		((IDisposable)_timer).Dispose();
	}
}