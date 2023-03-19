using System.Net;

using YoutubeExplode;
using YoutubeExplode.Search;

namespace Pomu;

[Obsolete("To be removed")]
public class Pomufier : IDisposable
{
	public YoutubeClient YoutubeClient => _client;


	private readonly HttpClient _httpClient;
	private readonly YoutubeClient _client;
	private bool _disposedValue;

	public Pomufier(CookieContainer cookies)
	{
		var handler = new HttpClientHandler()
		{
			CookieContainer = cookies
		};
		_httpClient = new HttpClient(handler);

		_client = new YoutubeClient(_httpClient);

		
	}

	public Pomufier() : this(new CookieContainer())
	{

	}

	public static Pomufier CreateFromCookieFile(string cookieFile)
	{
		var fileLines = File.ReadAllLines(cookieFile);

		var cookieContainer = new CookieContainer();
		for (int i = 0; i < fileLines.Length; i++)
		{
			var line = fileLines[i];
			if (line.StartsWith('#'))
				continue;
			if (line.Length <= 1)
				continue;

			var values = line.Split('\t');

			var cookie = new Cookie(values[4], values[5], values[2], values[0][1..])
			{
				Secure = values[3] == "TRUE"
			};
			cookieContainer.Add(cookie);
		}
		return new Pomufier(cookieContainer);
	}

	public async Task<string> LoadVideo(string url)
	{
		var video = await _client.Videos.GetAsync(url);
		return video.Title;
	}

	public async Task DownloadVideo(string url)
	{
		var video = await _client.Videos.GetAsync(url);
		var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id);
		var stream = manifest.GetMuxedStreams().First();
		await _client.Videos.Streams.DownloadAsync(stream, "pomu.mp4");
	}

	public IAsyncEnumerable<ChannelSearchResult> SearchChannels(string query)
	{
		return _client.Search.GetChannelsAsync(query);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_httpClient.Dispose();
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~Pomufier()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
