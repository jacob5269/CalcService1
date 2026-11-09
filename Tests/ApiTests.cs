using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string XmlContentType = "application/xml";
    private const string JsonContentType = "application/json";

    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculates_an_xml_request()
    {
        var response = await PostAsync("""
            <Maths>
              <Operation ID="Plus">
                <Value>2</Value>
                <Value>3</Value>
                <Operation ID="Multiplication">
                  <Value>4</Value>
                  <Value>5</Value>
                </Operation>
              </Operation>
            </Maths>
            """, XmlContentType, XmlContentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<Result>25</Result>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Calculates_a_json_request()
    {
        var response = await PostAsync("""
            {"operation":{"id":"Plus","values":[2,3],"operations":[{"id":"Multiplication","values":[4,5]}]}}
            """, JsonContentType, JsonContentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\": 25", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Calculates_the_flat_legacy_shape()
    {
        var response = await PostAsync("""{"operand1":10,"operand2":4,"operation":"divide"}""",
            JsonContentType, JsonContentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\": 2.5", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Returns_400_when_dividing_by_zero()
    {
        var response = await PostAsync(
            """<Maths><Operation ID="Division"><Value>1</Value><Value>0</Value></Operation></Maths>""",
            XmlContentType, XmlContentType);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Division by zero", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Returns_415_for_an_unsupported_content_type()
    {
        var response = await PostAsync("1 + 1", "text/plain", XmlContentType);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(string body, string contentType, string accept)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/calculate")
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        };
        request.Headers.Accept.ParseAdd(accept);

        return _client.SendAsync(request);
    }
}
