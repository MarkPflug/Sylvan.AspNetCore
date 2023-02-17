var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.Map(
	"/test", 
	 () => {
		var data = new[]
		{
			new
			{
				Id=1,
				Date = DateTime.UtcNow,
				Name = "Alpha",
				Value = 123.32m,
			},
			new
			{
				Id=2,
				Date = DateTime.UtcNow.AddDays(3),
				Name = "Beta",
				Value = 1023.77m,
			},
		};
		 
		return Results.Extensions.Excel(data);
	}
);

app.Run();
