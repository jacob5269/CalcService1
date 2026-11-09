using System.Text;
using System.Xml;
using System.Xml.Serialization;

public static class XmlBody
{
    public static async Task<T?> DeserializeAsync<T>(
        Stream body, CancellationToken cancellationToken = default)
    {
        // Kestrel disallows synchronous reads on the request body, so buffer it
        // asynchronously first, then deserialize from the in-memory stream.
        using var buffer = new MemoryStream();
        await body.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return Deserialize<T>(buffer);
    }

    public static T? Deserialize<T>(Stream body)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(body, settings);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(reader);
    }

    public static string Serialize<T>(T value)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", ""); // suppress xsi/xsd noise

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)
        }))
        {
            new XmlSerializer(typeof(T)).Serialize(writer, value, namespaces);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static IResult Ok<T>(T value) =>
        Results.Text(Serialize(value), "application/xml", Encoding.UTF8);

    public static IResult Error(int status, string message) =>
        Results.Text(Serialize(new ErrorResponse { Message = message }),
            "application/xml", Encoding.UTF8, status);
}