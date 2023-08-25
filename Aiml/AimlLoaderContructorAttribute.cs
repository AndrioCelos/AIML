namespace Aiml;

/// <summary>Indicates the constructor that should be used when deserialising a template element from AIML.</summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class AimlLoaderContructorAttribute : Attribute { }
