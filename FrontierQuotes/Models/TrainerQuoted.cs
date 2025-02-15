using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FrontierQuotes.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<TrainerQuoted>))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}

public record TrainerQuoted(string Class, string Name, string Sprite, string Greeting, string Win, string Loss);