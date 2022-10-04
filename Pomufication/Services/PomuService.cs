using Pomu;

using Pomufication.Models;

using System.Diagnostics;
using System.Text.Json;

using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Pomufication.Services;

public class PomuService : IDisposable
{
	public PomuConfig Config { get; private set; }
	private readonly Pomufier _pomufier;
	private readonly ILogger<PomuService> _logger;
	private readonly ChannelClient _ytChannels;
	private readonly SearchClient _ytSearch;
	private readonly VideoClient _ytVideos;
	private List<ActiveDownload> _activeDownloads;

	private Timer _timer;

	private bool _isSyncing;
	private bool _disposedValue;

	public PomuService(Pomufier pomufier, ILogger<PomuService> logger)
	{
		_pomufier = pomufier;
		_logger = logger;
		var youtube = pomufier.YoutubeClient;
		_ytChannels = youtube.Channels;
		_ytSearch = youtube.Search;
		_ytVideos = youtube.Videos;
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
		return _ytChannels.GetAsync(channelId);
	}

	public async Task<List<ChannelResultViewModel>> SearchChannels(string query, int maxResults = 50)
	{
		var results = await _ytSearch.GetChannelsAsync(query);
		return results.Take(maxResults).Select(r => new ChannelResultViewModel(r)).ToList();
	}

	private async Task<Process> StartStreamlinkAsync(string url)
	{
		var video = await _ytVideos.GetAsync(url);

		var fileName = CleanTitle(video);

		var filePath = Config.DataDirectory ?? "";

		if (!string.IsNullOrWhiteSpace(filePath) && !Directory.Exists(filePath))
		{
			_logger.LogWarning("Directory '{dir}' does not exists, falling back to working directory.", filePath);
			filePath = "";
		}

		filePath = Path.Combine(filePath, $"{fileName}.mp4");

		var processInfo = new ProcessStartInfo
		{
			FileName = "streamlink",
			Arguments = $"{url} best --stream-segment-timeout 60 --stream-timeout 360 --retry-streams 30 -o \"{filePath}\" {GetCoookieString()}",
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

	private static string CleanTitle(YoutubeExplode.Videos.Video video)
	{
		return $"{video.Author.ChannelTitle}_{video.Id}";
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
			var streams = await CheckForNewStreams();
			for (int i = 0; i < streams.Count; i++)
			{
				var url = streams[i];
				try
				{
					if (_activeDownloads.Any(d => d.Url == url))
						continue;
					_logger.LogInformation("Starting download of {url}", url);
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
				{
					download.Process.Dispose();
					_activeDownloads.RemoveAt(i--);
				}
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
			var channel = await _ytChannels.GetAsync(channelConfig.ChannelId);
			if (channel == null)
			{
				_logger.LogWarning("Could not find a channel with id '{Id}'. Skipping...", channelConfig.ChannelId);
				continue;
			}
			_logger.LogInformation($"Checking for streams: {channel.Title}");
			var upcomingStreams = await _ytSearch.GetResultsAsync(channel.Title)
				.Take(20)
				.Where(r => r is VideoSearchResult)
				.Cast<VideoSearchResult>()	
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

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_timer?.Dispose();
			}

			//Close spawned processes
			for (int i = 0; i < _activeDownloads.Count; i++)
			{
				if (!_activeDownloads[i].Process.HasExited)
					_activeDownloads[i].Process.Kill();
			}
			_disposedValue = true;
		}
	}

	~PomuService()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}