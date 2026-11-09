/// <summary>
/// A single mathematical operation. New operations can be added by
/// implementing this interface and registering the class in DI.
/// </summary>
public interface IMathOperation
{
    OperationType Type { get; }

    string Name { get; }

    /// <summary>
    /// Applies the operation to the already-evaluated operands.
    /// </summary>
    /// <exception cref="CalculationException">The operands are not valid for this operation.</exception>
    decimal Calculate(IReadOnlyList<decimal> values);
}

/// <summary>
/// Shared validation and plumbing for all operations.
/// </summary>
public abstract class MathOperationBase : IMathOperation
{
    public abstract OperationType Type { get; }

    public virtual string Name => Type.ToString();

    /// <summary>Whether the operation needs at least two operands.</summary>
    protected virtual bool RequiresTwoOperands => false;

    public decimal Calculate(IReadOnlyList<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            throw new MalformedExpressionException(
                "An operation must contain at least one value or inner operation.");

        if (RequiresTwoOperands && values.Count < 2)
            throw new MalformedExpressionException(
                $"{Name} requires at least two values.");

        return Apply(values);
    }

    protected abstract decimal Apply(IReadOnlyList<decimal> values);

    /// <summary>Left-to-right fold used by ordered operations.</summary>
    protected static decimal Fold(
        IReadOnlyList<decimal> values, Func<decimal, decimal, decimal> combine)
    {
        var result = values[0];
        for (var i = 1; i < values.Count; i++)
            result = combine(result, values[i]);

        return result;
    }
}

/// <summary>
/// Base for operations that combine any number of operands with an
/// associative operator (addition, multiplication).
/// </summary>
public abstract class NaryOperationBase : MathOperationBase
{
    protected abstract decimal Seed { get; }

    protected abstract decimal Combine(decimal left, decimal right);

    protected sealed override decimal Apply(IReadOnlyList<decimal> values)
    {
        var result = Seed;
        foreach (var value in values)
            result = Combine(result, value);

        return result;
    }
}

public sealed class Addition : NaryOperationBase
{
    public override OperationType Type => OperationType.Plus;
    protected override decimal Seed => 0m;
    protected override decimal Combine(decimal left, decimal right) => left + right;
}

public sealed class Multiplication : NaryOperationBase
{
    public override OperationType Type => OperationType.Multiplication;
    protected override decimal Seed => 1m;
    protected override decimal Combine(decimal left, decimal right) => left * right;
}

public sealed class Subtraction : MathOperationBase
{
    public override OperationType Type => OperationType.Minus;
    protected override bool RequiresTwoOperands => true;
    protected override decimal Apply(IReadOnlyList<decimal> values) =>
        Fold(values, (left, right) => left - right);
}

public sealed class Division : MathOperationBase
{
    public override OperationType Type => OperationType.Division;
    protected override bool RequiresTwoOperands => true;

    protected override decimal Apply(IReadOnlyList<decimal> values) =>
        Fold(values, (left, right) =>
            right == 0 ? throw new DivisionByZeroException() : left / right);
}

/// <summary>
/// Resolves an operation ID (enum name or common alias) to its implementation.
/// Adding an operation only requires a new class + DI registration.
/// </summary>
public interface IOperationRegistry
{
    IReadOnlyCollection<IMathOperation> All { get; }

    IMathOperation Get(string? id);
}

public sealed class OperationRegistry : IOperationRegistry
{
    private static readonly Dictionary<string, OperationType> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["plus"] = OperationType.Plus,
            ["add"] = OperationType.Plus,
            ["+"] = OperationType.Plus,
            ["minus"] = OperationType.Minus,
            ["subtract"] = OperationType.Minus,
            ["-"] = OperationType.Minus,
            ["multiplication"] = OperationType.Multiplication,
            ["multiply"] = OperationType.Multiplication,
            ["*"] = OperationType.Multiplication,
            ["x"] = OperationType.Multiplication,
            ["division"] = OperationType.Division,
            ["divide"] = OperationType.Division,
            ["/"] = OperationType.Division
        };

    private readonly Dictionary<OperationType, IMathOperation> _operations;

    public OperationRegistry(IEnumerable<IMathOperation> operations)
    {
        _operations = operations.ToDictionary(operation => operation.Type);
    }

    public IReadOnlyCollection<IMathOperation> All => _operations.Values;

    public IMathOperation Get(string? id)
    {
        if (id is null || id.Trim().Length == 0)
            throw new UnknownOperationException(null);

        return Aliases.TryGetValue(id.Trim(), out var type) &&
               _operations.TryGetValue(type, out var operation)
            ? operation
            : throw new UnknownOperationException(id);
    }
}
