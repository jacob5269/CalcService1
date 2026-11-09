public class OperationTests
{
    private static readonly ExpressionEvaluator Evaluator = new(
        new OperationRegistry([new Addition(), new Subtraction(), new Multiplication(), new Division()]));

    private static Operation Operation(string id, params decimal[] values) =>
        new() { ID = id, Values = [.. values] };

    private static decimal Evaluate(string id, params decimal[] values) =>
        Evaluator.Evaluate(Operation(id, values));

    [Fact]
    public void Addition_sums_the_values() =>
        Assert.Equal(5m, Evaluate("Plus", 2, 3));

    [Fact]
    public void Subtraction_subtracts_the_second_value() =>
        Assert.Equal(6m, Evaluate("Minus", 10, 4));

    [Fact]
    public void Multiplication_multiplies_the_values() =>
        Assert.Equal(42m, Evaluate("Multiplication", 6, 7));

    [Fact]
    public void Division_divides_by_the_second_value() =>
        Assert.Equal(4m, Evaluate("Division", 20, 5));

    [Fact]
    public void Evaluates_nested_operations()
    {
        // 2 + 3 + (4 x 5) = 25
        var expression = new Operation
        {
            ID = "Plus",
            Values = [2, 3],
            Operations = [Operation("Multiplication", 4, 5)]
        };

        Assert.Equal(25m, Evaluator.Evaluate(expression));
    }

    [Fact]
    public void Division_by_zero_throws() =>
        Assert.Throws<DivisionByZeroException>(() =>
            Evaluator.Evaluate(Operation("Division", 1, 0)));

    [Fact]
    public void Unknown_operation_throws() =>
        Assert.Throws<UnknownOperationException>(() =>
            Evaluator.Evaluate(Operation("Power", 2, 3)));
}
