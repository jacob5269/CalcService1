using System.Xml.Serialization;

[XmlRoot("Maths")]
public class Maths
{
    [XmlElement("Operation")] public Operation? Operation { get; set; }
}

[XmlRoot("Operation")]
public class Operation
{
    [XmlAttribute("ID")] public string? ID { get; set; }

    [XmlElement("Value")] public List<decimal> Values { get; set; } = new();

    [XmlElement("Operation")] public List<Operation> Operations { get; set; } = new();
}

/// <summary>
/// Alternative, flat request shape (<c>&lt;CalculateRequest&gt;</c> or
/// <c>{"operand1":10,"operand2":5,"operation":"Add"}</c>).
/// Normalised to <see cref="Maths"/> without touching the calculator.
/// </summary>
[XmlRoot("CalculateRequest")]
public class CalculateRequest
{
    [XmlElement("Operand1")] public decimal Operand1 { get; set; }

    [XmlElement("Operand2")] public decimal Operand2 { get; set; }

    [XmlElement("Operation")] public string Operation { get; set; } = "";

    public Maths ToMaths() => new()
    {
        Operation = new Operation
        {
            ID = Operation,
            Values = { Operand1, Operand2 }
        }
    };
}

[XmlRoot("CalculateResponse")]
public class CalculateResponse
{
    [XmlElement("Result")] public decimal Result { get; set; }
}

public class ErrorResponse
{
    [XmlElement("Message")] public string Message { get; set; } = "";
}
