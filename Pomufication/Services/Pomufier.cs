using System.Net;

namespace Pomu;

[Obsolete("To be removed")]
public class Pomufier
{

	private readonly HttpClient _httpClient;
	private bool _disposedValue;

	public Pomufier(CookieContainer cookies)
	{
		
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
}
