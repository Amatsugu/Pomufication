using Pomu;

using Pomufication.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if DEBUG
	builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#else
builder.Services.AddControllersWithViews();
#endif

var pomufier = Pomu.Pomufier.CreateFromCookieFile(@"D:\Downloads\youtube.com_cookies.txt");

builder.Services.AddSingleton<Pomu.Pomufier>(pomufier);
builder.Services.AddSingleton<PomuService>();
//builder.Services.AddSingleton<Pomu.Pomufier>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
