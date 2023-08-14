namespace Aiml;
/// <summary>Represents the outcome of an AIML unit test.</summary>
public class TestResult {
	/// <summary>Indicates whether the test passed.</summary>
	public bool Passed { get; }
	/// <summary>If the test failed, returns a message describing the reason for the failure. If the test passed, returns null.</summary>
	public string? Message { get; }
	/// <summary>Returns the duration of the test.</summary>
	public TimeSpan Duration { get; }

	public static TestResult Pass(TimeSpan duration) => new(true, null, duration);
	public static TestResult Failure(string message, TimeSpan duration) => new(false, message, duration);

	private TestResult(bool passed, string? message, TimeSpan duration) {
		this.Passed = passed;
		this.Message = message;
		this.Duration = duration;
	}
}
