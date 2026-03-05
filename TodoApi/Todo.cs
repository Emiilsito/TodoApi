using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TodoApi;

public class Todo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public int TareaCodigo { get; set; }

    public string? Name { get; set; }
    public string? Nota { get; set; }
    public bool Done { get; set; }
    public string? Tipo { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? FinalizadoEn { get; set; }
    public DateTime? FechaCompletado { get; set; }
    public List<Subtarea> Subtareas { get; set; } = new();
}

public class Subtarea
{
    public string? Nombre { get; set; }
    public bool Completada { get; set; }
}