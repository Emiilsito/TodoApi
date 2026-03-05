using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;
using TodoApi;
using TodoApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(connectionString);
var database = mongoClient.GetDatabase("TodoAppDb");
var todoCollection = database.GetCollection<Todo>("Tareas");

builder.Services.AddSingleton(todoCollection);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
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

app.MapHub<TodoHub>("/todohub");

var todoItems = app.MapGroup("/todoitems");

todoItems.MapGet("/", async (IMongoCollection<Todo> col) =>
    await col.Find(_ => true).ToListAsync());
    

todoItems.MapPost("/", async (Todo todo, IMongoCollection<Todo> col, IHubContext<TodoHub> hubContext) =>
{
    todo.Id = null;
    var ultimaTarea = await col.Find(_ => true).SortByDescending(t => t.TareaCodigo).FirstOrDefaultAsync();
    todo.TareaCodigo = (ultimaTarea == null) ? 1 : ultimaTarea.TareaCodigo + 1;
    todo.CreadoEn = DateTime.UtcNow;

    await col.InsertOneAsync(todo);
    await hubContext.Clients.All.SendAsync("ReceiveRefresh");

    return Results.Created($"/todoitems/{todo.TareaCodigo}", todo);
});

todoItems.MapPut("/{id:int}", async (int id, Todo inputTodo, IMongoCollection<Todo> col, IHubContext<TodoHub> hubContext) =>
{
    var tareaExistente = await col.Find(t => t.TareaCodigo == id).FirstOrDefaultAsync();
    if (tareaExistente == null) return Results.NotFound();

    inputTodo.Id = tareaExistente.Id;
    inputTodo.TareaCodigo = id;

    var result = await col.ReplaceOneAsync(t => t.TareaCodigo == id, inputTodo);

    if (result.ModifiedCount > 0)
    {
        await hubContext.Clients.All.SendAsync("ReceiveRefresh");
        return Results.NoContent();
    }
    return Results.NotFound();
});

todoItems.MapDelete("/{id:int}", async (int id, IMongoCollection<Todo> col, IHubContext<TodoHub> hubContext) =>
{
    var result = await col.DeleteOneAsync(t => t.TareaCodigo == id);
    if (result.DeletedCount > 0)
    {
        await hubContext.Clients.All.SendAsync("ReceiveRefresh");
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.Run();