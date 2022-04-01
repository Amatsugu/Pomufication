using Microsoft.IdentityModel.Tokens;

using Pomufication.Models;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Pomufication.Services;

public class AuthService
{
	public AuthInfo AuthInfo { get; private set; }
	private List<string> _activeTokens;

	private string? _code;

	public AuthService()
	{
		_activeTokens = new List<string>();
		if (File.Exists("jwt.json"))
		{
			AuthInfo = AuthInfo.Load("jwt.json");
		}
		else
		{
			AuthInfo = AuthInfo.Create("pomu", "pomudachi");
			AuthInfo.Save("jwt.json");
		}
	}

	public string GetLoginCode()
	{
		var gen = RandomNumberGenerator.GetBytes(8);
		_code = Convert.ToBase64String(gen);
		return _code;
	}

	public string? ExchangeCode(string code)
	{
		if (code != _code)
			return null;
		_code = null;
		return RequestNewToken();
	}

	public string RequestNewToken()
	{
		var id = new ClaimsIdentity();
		var handler = new JwtSecurityTokenHandler();
		var signCreds = new SigningCredentials(new SymmetricSecurityKey(AuthInfo.SecureKey), SecurityAlgorithms.HmacSha256);
		var token = handler.CreateEncodedJwt(AuthInfo.Issuer, AuthInfo.Audience, id, notBefore: DateTime.Now, expires: null, issuedAt: DateTime.Now, signingCredentials: signCreds);
		_activeTokens.Add(token);
		return token;
	}

	public bool VerifyToken(string token)
	{
		return _activeTokens.Contains(token);
	}
}