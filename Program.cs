using System.Xml;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// One class per mathematical operation. Adding a new operation only requires
// a new IMathOperation implementation and a registration here.
builder.Services.AddSingleton<IMathOperation, Addition>();
builder.Services.AddSingleton<IMathOperation, Subtraction>();
builder.Services.AddSingleton<IMathOperation, Multiplication>();
builder.Services.AddSingleton<IMathOperation, Division>();
builder.Services.AddSingleton<IOperationRegistry, OperationRegistry>();
builder.Services.AddSingleton<ExpressionEvaluator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/calculate", async (
    HttpRequest request,
    ExpressionEvaluator evaluator,
    CancellationToken cancellationToken) =>
{
    try
    {
        var maths = await BodySerializer.ReadMathsAsync(request, cancellationToken);
        var result = evaluator.Evaluate(maths.Operation);
        return BodySerializer.Ok(request, new CalculateResponse { Result = result });
    }
    catch (MalformedRequestException ex)
    {
        return BodySerializer.Error(request, ex.StatusCode, ex.Message);
    }
    catch (CalculationException ex)
    {
        // Anything thrown by an operation or by the evaluator is a client error.
        return BodySerializer.Error(request, StatusCodes.Status400BadRequest, ex.Message);
    }
})
.Produces<CalculateResponse>(StatusCodes.Status200OK, BodySerializer.XmlContentType, BodySerializer.JsonContentType)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest, BodySerializer.XmlContentType, BodySerializer.JsonContentType)
.Produces<ErrorResponse>(StatusCodes.Status415UnsupportedMediaType, BodySerializer.XmlContentType, BodySerializer.JsonContentType);

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Exposed so the test project can host the app with WebApplicationFactory<Program>.
public partial class Program { }
