using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Pomu;

using Pomufication.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if DEBUG
	builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#else
builder.Services.AddControllersWithViews();
#endif
var cookiesFile = @"D:\Downloads\youtube.com_cookies.txt";
var pomufier = File.Exists(cookiesFile) ? Pomu.Pomufier.CreateFromCookieFile(cookiesFile) : new Pomufier();

builder.Services.AddSingleton<Pomu.Pomufier>(pomufier);
builder.Services.AddSingleton<PomuService>();
var auth = new AuthService();

builder.Services.AddSingleton<AuthService>(auth);


var signingKey = new SymmetricSecurityKey(auth.AuthInfo.SecureKey);

var validationParams = new TokenValidationParameters
{
	ValidateIssuerSigningKey = true,
	IssuerSigningKey = signingKey,
	ValidateIssuer = true,
	ValidIssuer = auth.AuthInfo.Issuer,
	ValidateAudience = true,
	ValidAudience = auth.AuthInfo.Audience,
	ValidateLifetime = false,
	ClockSkew = TimeSpan.FromMinutes(1),
};

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => //Bearer auth
{
	options.TokenValidationParameters = validationParams;
	options.Events = new JwtBearerEvents
	{
		OnMessageReceived = ctx =>
		{
			//Extract token from cookie
			if (string.IsNullOrWhiteSpace(ctx.Token))
				ctx.Token = ctx.Request.Cookies["token"];
			//Extract token from Bearer
			if (string.IsNullOrWhiteSpace(ctx.Token))
			{
				var t = ctx.Request.Headers["Authorization"].FirstOrDefault();
				if (t == null || t.Length <= 7)
					ctx.Token = null;
				else
					ctx.Token = t?[7..];
			}

			return Task.CompletedTask;
		},
		OnAuthenticationFailed = ctx =>
		{
			//Clear login cookie and forward challege
			ctx.Response.Cookies.Append("token", "", new CookieOptions
			{
				MaxAge = TimeSpan.Zero,
				Expires = DateTime.Now
			});
			ctx.Options.ForwardChallenge = CookieAuthenticationDefaults.AuthenticationScheme;

			return Task.CompletedTask;
		}
	};
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
	options.AccessDeniedPath = "/login";
	options.LoginPath = "/login";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseStaticFiles();


app.Services.GetService<PomuService>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
