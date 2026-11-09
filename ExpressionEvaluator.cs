/// <summary>
/// Evaluates a (possibly nested) operation tree recursively.
/// </summary>
public sealed class ExpressionEvaluator(IOperationRegistry registry)
{
    /// <summary>
    /// Hard limit on nesting so hostile input cannot blow the stack.
    /// </summary>
    public const int MaxDepth = 64;

    public decimal Evaluate(Operation? operation) => Evaluate(operation, depth: 0);

    private decimal Evaluate(Operation? operation, int depth)
    {
        if (operation is null)
            throw new MalformedExpressionException("Request does not contain an operation.");

        if (depth > MaxDepth)
            throw new DepthExceededException(MaxDepth);

        var mathOperation = registry.Get(operation.ID);

        var values = new List<decimal>(operation.Values.Count + operation.Operations.Count);
        values.AddRange(operation.Values);

        // Deepest-first: solve every nested operation before applying this one.
        foreach (var child in operation.Operations)
            values.Add(Evaluate(child, depth + 1));

        return mathOperation.Calculate(values);
    }
}
