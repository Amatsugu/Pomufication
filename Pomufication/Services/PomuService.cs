﻿using Pomufication.Models;
using Pomufication.Models.Youtube;

using System.Diagnostics;
using System.Text.Json;


namespace Pomufication.Services;

public class PomuService : IHostedService
{
	public PomuConfig Config { get; private set; }

	private readonly YouTubeService _youTube;
	private readonly ILogger<PomuService> _logger;
	private List<ActiveDownload> _activeDownloads;

	private List<VideoInfo> _pendingDownloads;

	private Timer? _timer;

	private bool _isSyncing;
	private bool _disposedValue;

	public PomuService(YouTubeService youTube, ILogger<PomuService> logger)
	{
		_youTube = youTube;
		_logger = logger;
		_activeDownloads = new List<ActiveDownload>();
		_pendingDownloads = new List<VideoInfo>();
		Config = LoadConfig();
	}

	private record ActiveDownload(string Url, Process Process);

	private Timer StartTimer()
	{
		return new Timer(Sync, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
	}

	public Task<List<ChannelInfo>> SearchChannels(string query, int maxResults = 50)
	{
		return _youTube.SearchChannelsAsync(query);
	}

	private Process StartStreamlink(VideoInfo info)
	{
		var fileName = CleanTitle(info);

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
			Arguments = $"\"{info.Url}\" best --stream-segment-timeout 60 --stream-timeout 360 --retry-streams 30 -o \"{filePath}\"",
		};
		var p = Process.Start(processInfo) ?? throw new Exception("Failed to start streamlink");
		return p;
	}

	private string? GetCoookieString()
	{
		if (!File.Exists("cookies.txt"))
			return null;
		//TODO: Generate cookie string
		return null;
	}

	private static string CleanTitle(VideoInfo video)
	{
		var cleanName = video.Title.Replace("\\", "")
			.Replace("/", "")
			.Replace("*", "")
			.Replace("?", "")
			.Replace("\"", "")
			.Replace("<", "")
			.Replace(">", "")
			.Replace("|", "")
			.Replace(":", "");
		return $"{video.Channel.Name}_{video.Id}_{cleanName}";
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
			
			await ProcessCheckingForStreamsAsync();
			ProcessDownloadQueue();
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

	private async Task ProcessCheckingForStreamsAsync()
	{
		var streams = await CheckForNewStreams();
		for (int i = 0; i < streams.Count; i++)
		{
			var streamInfo = streams[i];
			if (_activeDownloads.Any(d => d.Url == streamInfo.Url))
				continue;
			_logger.LogInformation("Queueing download of {url}", streamInfo);
			_pendingDownloads.Add(streamInfo);
		}
	}

	private void ProcessDownloadQueue()
	{
		for (int i = 0; i < _activeDownloads.Count; i++)
		{
			var download = _activeDownloads[i];
			if (download.Process.HasExited)
			{
				download.Process.Dispose();
				_activeDownloads.RemoveAt(i--);
			}
		}
		for (int i = 0; i < _pendingDownloads.Count; i++)
		{
			var stream = _pendingDownloads[i];
			var eta = stream.StartTime - DateTimeOffset.Now;
			if (Math.Abs(eta.TotalMinutes) > 30)
				continue;
			try
			{
				var dl = StartStreamlink(stream);
				_activeDownloads.Add(new(stream.Url, dl));
				_pendingDownloads.RemoveAt(i--);
				_logger.LogInformation("Starting download of {url}", stream.Url);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to donwload stream. {url}", stream.Url);
			}

		}
	}

	public async Task<List<VideoInfo>> CheckForNewStreams()
	{
		var foundStreams = new List<VideoInfo>();

		for (int i = 0; i < Config.Channels.Count; i++)
		{
			var channelConfig = Config.Channels[i];
			try
			{
				if (!channelConfig.Enabled)
					continue;
				var channel = await _youTube.GetChannelInfoAsync(channelConfig.ChannelId);
				if (channel == null)
				{
					_logger.LogWarning("Could not find a channel with id '{Id}'. Skipping...", channelConfig.ChannelId);
					continue;
				}
				_logger.LogInformation("Checking for streams: {channel.Name}", channel.Name);
				var upcomingStreams = await _youTube.GetUpcomingStreamsAsync(channel.Id);
				var matchingStreams = upcomingStreams.Where(v => channelConfig.FilterKeywords.All(k => k.Match(v.Title)));

				if (matchingStreams.Any())
					foundStreams.AddRange(matchingStreams);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Failed to load streams for {channel}", channelConfig.ChannelId);
			}
			await Task.Delay(TimeSpan.FromSeconds(1));
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

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Pomu Service Stopping...");
		_timer?.Dispose();
		return Task.CompletedTask;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Pomu Service Starting...");
		_timer = StartTimer();
		return Task.CompletedTask;
	}
}