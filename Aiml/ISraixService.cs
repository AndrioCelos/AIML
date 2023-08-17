﻿using System.Xml;

namespace Aiml; 
/// <summary>Implements a handler for the AIML <c>sraix</c> template element.</summary>
public interface ISraixService {
	/// <summary>Processes a request and returns the result.</summary>
	/// <param name="text">The evaluated contents of the <c>sraix</c> element.</param>
	/// <param name="attributes">The attributes of the <c>sraix</c> element.</param>
	/// <param name="process">A <see cref="RequestProcess"/> instance providing information about the request currently being processed.</param>
	/// <returns>The text that should replace the <c>sraix</c> element.</returns>
	string Process(string text, XmlAttributeCollection attributes, RequestProcess process);
}
