using System.IO.Pipelines;
using System.Text.Json;
using JsonReaders;

if (args.Length is 0)
{
    Console.WriteLine("Please provide a file path.");
    return;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.WriteLine("The file does not exist.");
    return;
}

// Load file
var stream = File.OpenRead(filePath);

// Attach stream to Pipe
var pipe = new Pipe();
var reader = pipe.Reader;
_ = CopyToWriter(pipe, stream);
var result = await reader.ReadAsync();

// Create JsonPipeReader
var jsonReader = new JsonPipeReader(reader, result);
var memoryStream = new MemoryStream();
var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });
int depth = -1;

while (depth != 0)
{
    IdentityWrite(jsonReader, writer);
    await jsonReader.AdvanceAsync();
    depth = jsonReader.Depth;
}

writer.Flush();
memoryStream.Position = 0;
WriteJsonToConsole(memoryStream);

static async Task CopyToWriter(Pipe pipe, Stream release)
{
    await release.CopyToAsync(pipe.Writer);
    pipe.Writer.Complete();
}

static void IdentityWrite(JsonPipeReader jsonReader, Utf8JsonWriter writer)
{
    Utf8JsonReader reader = jsonReader.GetReader();
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                break;
            case JsonTokenType.EndObject:
                writer.WriteEndObject();
                break;
            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                break;
            case JsonTokenType.EndArray:
                writer.WriteEndArray();
                break;
            case JsonTokenType.PropertyName:
                string name = reader.GetString() ?? "";
                writer.WritePropertyName(name);
                break;
            case JsonTokenType.String:
                string s = reader.GetString() ?? "";
                writer.WriteStringValue(s);
                break;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int int32))
                {
                    writer.WriteNumberValue(int32);
                }
                else if (reader.TryGetInt64(out long int64))
                {
                    writer.WriteNumberValue(int64);
                }
                else if (reader.TryGetDouble(out double dbl))
                {
                    writer.WriteNumberValue(dbl);
                }
                else if (reader.TryGetDecimal(out decimal dec))
                {
                    writer.WriteNumberValue(dec);
                }
                break;
            case JsonTokenType.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonTokenType.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonTokenType.Null:
                writer.WriteNullValue();
                break;
            case JsonTokenType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    jsonReader.UpdateState(reader);
}

static void WriteJsonToConsole(Stream stream)
{
    for (int i = 0; i < stream.Length; i++)
    {
        Console.Write((char)stream.ReadByte());
    }

    Console.WriteLine();
}
