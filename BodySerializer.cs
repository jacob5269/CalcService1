using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

/// <summary>
/// Reads a request body as XML or JSON and writes responses in the format
/// the client asked for (Accept header).
/// </summary>
public static class BodySerializer
{
    public const string XmlContentType = "application/xml";
    public const string JsonContentType = "application/json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = 1_000_000
    };

    /// <summary>
    /// Reads either the nested shape (<c>Maths</c>) or the flat shape
    /// (<c>CalculateRequest</c>) from XML or JSON and normalises it.
    /// </summary>
    public static async Task<Maths> ReadMathsAsync(
        HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
            throw new MalformedRequestException("Empty request body.");

        var body = buffer.ToArray();
        return IsJson(request) ? ReadJson(body) : ReadXml(body);
    }

    public static IResult Ok(HttpRequest request, CalculateResponse response) =>
        Write(request, StatusCodes.Status200OK, response);

    public static IResult Error(HttpRequest request, int statusCode, string message) =>
        Write(request, statusCode, new ErrorResponse { Message = message });

    private static IResult Write<T>(HttpRequest request, int statusCode, T value)
    {
        if (WantsJson(request))
        {
            return Results.Text(JsonSerializer.Serialize(value, JsonOptions),
                $"{JsonContentType}; charset=utf-8", Encoding.UTF8, statusCode);
        }

        return Results.Text(XmlBody.Serialize(value),
            $"{XmlContentType}; charset=utf-8", Encoding.UTF8, statusCode);
    }

    private static bool IsJson(HttpRequest request)
    {
        var mediaType = (request.ContentType ?? "")
            .Split(';', 2)[0]
            .Trim();

        if (mediaType.Length == 0)
            return false; // no content type: keep the original XML behaviour

        if (mediaType.EndsWith("json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (mediaType.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
            return false;

        throw new MalformedRequestException(
            $"Unsupported content type '{mediaType}'. Use {XmlContentType} or {JsonContentType}.",
            StatusCodes.Status415UnsupportedMediaType);
    }

    private static bool WantsJson(HttpRequest request) =>
        request.Headers.Accept.ToString().Contains("json", StringComparison.OrdinalIgnoreCase);

    private static Maths ReadXml(byte[] body)
    {
        try
        {
            var root = FindRootElement(body);

            return root switch
            {
                "Maths" => DeserializeXml<Maths>(body),
                "Operation" => new Maths { Operation = DeserializeXml<Operation>(body) },
                "CalculateRequest" => DeserializeXml<CalculateRequest>(body).ToMaths(),
                _ => throw new MalformedRequestException(
                    $"Unsupported root element '<{root}>'. Expected <Maths>, <Operation>, or <CalculateRequest>.")
            };
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            throw new MalformedRequestException($"Request body is not valid XML: {ex.Message}");
        }
    }

    private static Maths ReadJson(byte[] body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new MalformedRequestException("Request body must be a JSON object.");

            // Flat shape: {"operand1":10,"operand2":5,"operation":"Add"}
            if (HasProperty(root, "operand1") || HasProperty(root, "operand2"))
            {
                var flat = JsonSerializer.Deserialize<CalculateRequest>(body, JsonOptions)
                    ?? throw new MalformedRequestException("Request body is not valid JSON.");
                return flat.ToMaths();
            }

            // Nested shape: {"operation":{"id":"Plus","values":[1,2]}}
            if (HasProperty(root, "operation"))
            {
                var maths = JsonSerializer.Deserialize<Maths>(body, JsonOptions)
                    ?? throw new MalformedRequestException("Request body is not valid JSON.");
                return maths;
            }

            throw new MalformedRequestException(
                "Unsupported JSON shape. Send an 'operation' object or 'operand1'/'operand2' properties.");
        }
        catch (JsonException ex)
        {
            throw new MalformedRequestException($"Request body is not valid JSON: {ex.Message}");
        }
    }

    private static bool HasProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string FindRootElement(byte[] body)
    {
        using var reader = XmlReader.Create(new MemoryStream(body), XmlSettings);

        if (reader.MoveToContent() != XmlNodeType.Element)
            throw new MalformedRequestException("Request body is not valid XML.");

        return reader.LocalName;
    }

    private static T DeserializeXml<T>(byte[] body) =>
        XmlBody.Deserialize<T>(new MemoryStream(body))
        ?? throw new MalformedRequestException("Request body is not valid XML.");
}
