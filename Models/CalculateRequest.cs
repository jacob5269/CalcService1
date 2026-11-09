using System.Xml.Serialization;

[XmlRoot("CalculateRequest")]
public class CalculateRequest
{
    [XmlElement("Operand1")] public decimal Operand1 { get; set; }
    [XmlElement("Operand2")] public decimal Operand2 { get; set; }
    [XmlElement("Operation")] public string Operation { get; set; } = "";
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