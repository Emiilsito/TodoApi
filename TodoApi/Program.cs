using MongoDB.Driver;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("No se encontró la cadena de conexión 'MongoDb' en appsettings.json");
}

var mongoClient = new MongoClient(connectionString);
var database = mongoClient.GetDatabase("TodoAppDb");
var todoCollection = database.GetCollection<Todo>("Tareas");

builder.Services.AddSingleton(todoCollection);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();

var todoItems = app.MapGroup("/todoitems");

todoItems.MapGet("/", async (IMongoCollection<Todo> col) =>
    await col.Find(_ => true).ToListAsync());

todoItems.MapGet("/{id:int}", async (int id, IMongoCollection<Todo> col) =>
    await col.Find(t => t.TareaCodigo == id).FirstOrDefaultAsync()
        is Todo todo ? Results.Ok(todo) : Results.NotFound());

todoItems.MapPost("/", async (Todo todo, IMongoCollection<Todo> col) =>
{
    todo.Id = null;
    var ultimaTarea = await col.Find(_ => true).SortByDescending(t => t.TareaCodigo).FirstOrDefaultAsync();
    todo.TareaCodigo = (ultimaTarea == null) ? 1 : ultimaTarea.TareaCodigo + 1;
    todo.CreadoEn = DateTime.UtcNow;

    await col.InsertOneAsync(todo);
    return Results.Created($"/todoitems/{todo.TareaCodigo}", todo);
});

todoItems.MapPut("/{id:int}", async (int id, Todo inputTodo, IMongoCollection<Todo> col) =>
{
    var tareaExistente = await col.Find(t => t.TareaCodigo == id).FirstOrDefaultAsync();
    if (tareaExistente == null) return Results.NotFound();

    inputTodo.Id = tareaExistente.Id;
    inputTodo.TareaCodigo = id;

    var result = await col.ReplaceOneAsync(t => t.TareaCodigo == id, inputTodo);
    return result.ModifiedCount > 0 ? Results.NoContent() : Results.NotFound();
});

todoItems.MapDelete("/{id:int}", async (int id, IMongoCollection<Todo> col) =>
{
    var result = await col.DeleteOneAsync(t => t.TareaCodigo == id);
    return result.DeletedCount > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();