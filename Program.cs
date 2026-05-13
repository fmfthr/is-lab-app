using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var notes = new List<Note>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        time = DateTime.UtcNow
    });
});

app.MapGet("/version", (IConfiguration config) =>
{
    var appName = config["App:Name"] ?? "IsLabApp";
    var appVersion = config["App:Version"] ?? "0.1.0-lab11";

    return Results.Ok(new
    {
        name = appName,
        version = appVersion
    });
});

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.BadRequest(new
        {
            status = "error",
            message = "Connection string 'Mssql' is not configured."
        });
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        return Results.Ok(new
        {
            status = "ok",
            message = "Database connection successful."
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Database connection failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.MapGet("/api/notes", () =>
{
    return Results.Ok(notes);
});

app.MapGet("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    return note is null
        ? Results.NotFound(new { message = $"Note with id={id} not found." })
        : Results.Ok(note);
});

app.MapPost("/api/notes", (CreateNoteRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "Title is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { message = "Text is required." });
    }

    var newId = notes.Count == 0 ? 1 : notes.Max(n => n.Id) + 1;

    var note = new Note
    {
        Id = newId,
        Title = request.Title.Trim(),
        Text = request.Text.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    notes.Add(note);

    return Results.Created($"/api/notes/{note.Id}", note);
});

app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    if (note is null)
    {
        return Results.NotFound(new { message = $"Note with id={id} not found." });
    }

    notes.Remove(note);

    return Results.Ok(new
    {
        message = $"Note with id={id} deleted."
    });
});

app.Run();

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
