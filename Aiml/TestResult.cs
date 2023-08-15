using System.Diagnostics.CodeAnalysis;

namespace Aiml;
/// <summary>Represents the outcome of an AIML unit test.</summary>
public class TestResult {
	/// <summary>Indicates whether the test passed.</summary>
	public bool Passed => this.Message is null;
	/// <summary>If the test failed, returns a message describing the reason for the failure. If the test passed, returns null.</summary>
	public string? Message { get; }
	/// <summary>Returns the duration of the test.</summary>
	public TimeSpan Duration { get; }

	public static TestResult Pass(TimeSpan duration) => new(null, duration);
	public static TestResult Failure(string message, TimeSpan duration) => new(message, duration);

	private TestResult(string? message, TimeSpan duration) {
		this.Message = message;
		this.Duration = duration;
	}

#if NET5_0_OR_GREATER || NETCOREAPP2_1_OR_GREATER
	public bool IsPass([MaybeNullWhen(true)] out string message) {
		message = this.Message;
		return this.Passed;
	}
	public bool IsFailure([MaybeNullWhen(false)] out string message) {
		message = this.Message;
		return !this.Passed;
	}
#endif
}
