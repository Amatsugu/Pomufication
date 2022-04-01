using Newtonsoft.Json;

using System.Security.Cryptography;

namespace Pomufication.Models;

public struct AuthInfo
{
	public string Issuer;
	public string Audience;
	public byte[] SecureKey;

	/// <summary>
	/// Save this auth into in a json format to the sepcified file
	/// </summary>
	/// <param name="path">File path</param>
	/// <returns></returns>
	public AuthInfo Save(string path)
	{
		File.WriteAllText(path, JsonConvert.SerializeObject(this));
		return this;
	}

	/// <summary>
	/// Generate a new Auth Info with newly generated keys
	/// </summary>
	/// <param name="issuer"></param>
	/// <param name="audience"></param>
	/// <returns></returns>
	public static AuthInfo Create(string issuer, string audience)
	{
		var auth = new AuthInfo
		{
			Issuer = issuer,
			Audience = audience,
			SecureKey = GenetateJWTKey()
		};
		return auth;
	}

	/// <summary>
	/// Load auth info from a json file
	/// </summary>
	/// <param name="path">File path</param>
	/// <returns></returns>
	internal static AuthInfo Load(string path)
	{
		var jsonData = File.ReadAllText(path);
		var result = JsonConvert.DeserializeObject<AuthInfo>(jsonData);
		return result;
	}

	/// <summary>
	/// Generate a new key for use by JWT
	/// </summary>
	/// <returns></returns>
	public static byte[] GenetateJWTKey(int size = 64)
	{
		var key = RandomNumberGenerator.GetBytes(size);
		return key;
	}
}
