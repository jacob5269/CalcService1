using System.Xml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/calculate", async (HttpRequest request) =>
{
    CalculateRequest? input;
    try
    {
        input = await XmlBody.DeserializeAsync<CalculateRequest>(
            request.Body, request.HttpContext.RequestAborted);
    }
    catch (Exception ex) when (ex is XmlException or InvalidOperationException)
    {
        return XmlBody.Error(StatusCodes.Status400BadRequest,
            "Request body is not valid CalculateRequest XML.");
    }

    if (input is null)
        return XmlBody.Error(StatusCodes.Status400BadRequest, "Empty request body.");

    if (!Calculator.TryCalculate(input.Operand1, input.Operand2,
            input.Operation, out var result, out var error))
    {
        return XmlBody.Error(StatusCodes.Status400BadRequest, error);
    }

    return XmlBody.Ok(new CalculateResponse { Result = result });
});

app.Run();