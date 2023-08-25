using System.Xml;

namespace Aiml.Tests.TestExtension;
public class TestCustomTag(XmlElement element, XmlAttributeCollection attributes, TemplateElementCollection value1, TemplateElementCollection? value2) : TemplateNode
{
    public XmlElement Element { get; } = element;
    public XmlAttributeCollection Attributes { get; } = attributes;
    public TemplateElementCollection Value1 { get; } = value1;
    public TemplateElementCollection? Value2 { get; } = value2;

    public override string Evaluate(RequestProcess process) => $"{this.Value1.Evaluate(process)} {this.Value2?.Evaluate(process)}";
}
