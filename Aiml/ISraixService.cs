using System.Xml;

namespace Aiml; 
/// <summary>Implements a handler for the AIML <code>sraix</code> template element.</summary>
public interface ISraixService {
	/// <summary>Processes a request and returns the result.</summary>
	/// <param name="text">The evaluated contents of the <code>sraix</code> element.</param>
	/// <param name="attributes">The attributes of the <code>sraix</code> element.</param>
	/// <param name="subRequest">The sub-request currently being processed.</param>
	string Process(string text, XmlAttributeCollection attributes, RequestProcess process);
}
