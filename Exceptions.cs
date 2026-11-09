/// <summary>
/// Base type for every error that is the caller's fault (bad request).
/// Anything else is treated as a server error.
/// </summary>
public abstract class CalculationException : Exception
{
    protected CalculationException(string message) : base(message) { }
}

/// <summary>The expression is structurally invalid (empty, missing values, ...).</summary>
public sealed class MalformedExpressionException : CalculationException
{
    public MalformedExpressionException(string message) : base(message) { }
}

/// <summary>The operation ID is missing or is not a known operation.</summary>
public sealed class UnknownOperationException : CalculationException
{
    public string? Operation { get; }

    public UnknownOperationException(string? operation)
        : base(operation is null || operation.Trim().Length == 0
            ? "Operation is missing an 'ID' attribute."
            : $"Unknown operation '{operation}'. Use Plus, Minus, Multiplication, or Division.")
    {
        Operation = operation;
    }
}

/// <summary>A division operand was zero.</summary>
public sealed class DivisionByZeroException : CalculationException
{
    public DivisionByZeroException() : base("Division by zero is not allowed.") { }
}

/// <summary>The expression nests deeper than the evaluator allows.</summary>
public sealed class DepthExceededException : CalculationException
{
    public DepthExceededException(int maxDepth)
        : base($"Operation nesting exceeds the maximum depth of {maxDepth}.") { }
}

/// <summary>The request body could not be read as XML or JSON.</summary>
public sealed class MalformedRequestException : CalculationException
{
    public int StatusCode { get; }

    public MalformedRequestException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
