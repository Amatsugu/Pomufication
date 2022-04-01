using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Pomufication.Services;

public class PomuTokenValidator : JwtSecurityTokenHandler
{
	private readonly AuthService _authService;

	public PomuTokenValidator(AuthService authService)
	{
		_authService = authService;
	}

	public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
	{
		return base.ValidateToken(token, validationParameters, out validatedToken);
	}
}
