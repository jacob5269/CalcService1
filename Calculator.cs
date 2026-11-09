public static class Calculator
{
    public static bool TryCalculate(
        decimal a, decimal b, string operation,
        out decimal result, out string error)
    {
        error = "";
        switch (operation.Trim().ToLowerInvariant())
        {
            case "add":      result = a + b; return true;
            case "subtract": result = a - b; return true;
            case "multiply": result = a * b; return true;
            case "divide":
                if (b == 0) { result = 0; error = "Division by zero is not allowed."; return false; }
                result = a / b; return true;
            default:
                result = 0;
                error = $"Unknown operation '{operation}'. Use Add, Subtract, Multiply, or Divide.";
                return false;
        }
    }
}