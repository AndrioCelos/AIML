namespace Aiml {
	/// <summary>
	/// Represents a triple: an item of knowledge for an AIML 2.1 bot that consists of a subject, predicate and object.
	/// </summary>
	public class Triple {
		public string Subject { get; }
		public string Predicate { get; }
		public string Object { get; }

		public Triple(string subj, string pred, string obj) {
			this.Subject = subj;
			this.Predicate = pred;
			this.Object = obj;
		}
	}
}
