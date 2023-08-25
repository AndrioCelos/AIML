using System.Xml.Linq;

namespace Aiml.Tests.TestExtension;
public class TestCustomTag(XElement element, TemplateElementCollection value1, TemplateElementCollection? value2) : TemplateNode
{
    public XElement Element { get; } = element;
    public TemplateElementCollection Value1 { get; } = value1;
    public TemplateElementCollection? Value2 { get; } = value2;

    public override string Evaluate(RequestProcess process) => $"{this.Value1.Evaluate(process)} {this.Value2?.Evaluate(process)}";
}
