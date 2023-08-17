using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aiml;
public class Config {
	/// <summary>The value returned by the <c>get</c> and <c>bot</c> template elements for an unbound predicate, if no default value is defined for it.</summary>
	public string DefaultPredicate = "unknown";
	/// <summary>The value returned by the <c>map</c> template element if the map cannot be found or the input key is not in the map.</summary>
	public string DefaultMap = "unknown";
	/// <summary>The value returned by the <c>request</c> and <c>response</c> template elements if the index is beyond the history.</summary>
	public string DefaultHistory = "nil";
	/// <summary>The value returned by the <c>first</c> and <c>rest</c> template elements if the sought words do not exist.</summary>
	public string DefaultListItem = "nil";
	/// <summary>The value returned by the <c>uniq</c> template element if no matching triple is found.</summary>
	public string DefaultTriple = "nil";
	/// <summary>The value returned by the <c>star</c>, <c>thatstar</c> and <c>topicstar</c> template elements for a zero-length match.</summary>
	public string DefaultWildcard = "nil";

	/// <summary>The maximum number of requests and responses that will be remembered for each user.</summary>
	public int HistorySize = 16;

	/// <summary>The response the bot will give to input that doesn't match any AIML category.</summary>
	public string DefaultResponse = "I have no answer for that.";
	/// <summary>The maximum time, in milliseconds, a request should be allowed to run for.</summary>
	public double Timeout = 10e+3;
	/// <summary>The response to a request that times out.</summary>
	public string TimeoutMessage = "That query took too long for me to process.";
	/// <summary>The maximum allowed number of recursive <c>srai</c> template element calls.</summary>
	public int RecursionLimit = 50;
	/// <summary>The response to a request that exceeds the recursion limit.</summary>
	public string RecursionLimitMessage = "Too much recursion in AIML.";
	/// <summary>The maximum allowed number of loops on a single <c>condition</c> template element.</summary>
	public int LoopLimit = 100;
	/// <summary>The response to a request that exceeds the loop limit.</summary>
	public string LoopLimitMessage = "Too much looping in condition.";

	public int DefaultDelay = 1000;

	private CultureInfo locale = CultureInfo.CurrentCulture;
	/// <summary>
	/// The locale that should be used by default for string comparisons and the <c>date</c> template element.
	/// Defaults to the system's current locale.
	/// </summary>
	public CultureInfo Locale {
		get => this.locale;
		set {
			this.locale = value ?? CultureInfo.CurrentCulture;
			this.StringComparer = StringComparer.Create(this.Locale, true);
			this.CaseSensitiveStringComparer = StringComparer.Create(this.Locale, false);
			this.RebuildDictionaries();
		}
	}
	[JsonIgnore]
	/// <summary>Returns the <see cref="StringComparer"/> used for set and map comparisons. This is changed by setting the <see cref="Locale"/> property.</summary>
	public StringComparer StringComparer { get; private set; } = StringComparer.CurrentCultureIgnoreCase;

	[JsonIgnore]
	/// <summary>Returns the <see cref="StringComparer"/> used for test comparisons. This is changed by setting the <see cref="Locale"/> property.</summary>
	public StringComparer CaseSensitiveStringComparer { get; private set; } = StringComparer.CurrentCulture;

	/// <summary>The minimum level of messages that should be logged.</summary>
	public LogLevel LogLevel = LogLevel.Info;
	/// <summary>The maximum allowed number of recursive <c>srai</c> template elements that will have diagnostic messages logged.</summary>
	public int LogRecursionLimit = 2;

	/// <summary>The directory in which to look for AIML files. Defaults to '$ConfigDirectory/aiml'.</summary>
	public string AimlDirectory = "aiml";
	/// <summary>The directory in which to write logs. Defaults to '$ConfigDirectory/logs'.</summary>
	public string LogDirectory = "logs";
	/// <summary>The directory in which to look for sets. Defaults to '$ConfigDirectory/sets'.</summary>
	public string SetsDirectory = "sets";
	/// <summary>The directory in which to look for maps. Defaults to '$ConfigDirectory/maps'.</summary>
	public string MapsDirectory = "maps";

	/// <summary>The file path to which to save categories learned by the <c>learnf</c> template element.</summary>
	public string LearnfFile { get; set; } = "learnf.aiml";

	/// <summary>Defined strings that delimit sentences in requests and responses. Defaults to [ ".", "!", "?", ";" ].</summary>
	public char[] Splitters = new[] { '.', '!', '?', ';' };

	/// <summary>If this is true, using the <c>set</c> template element to set a predicate or variable to the default value will unbind it instead.</summary>
	public bool UnbindPredicatesWithDefaultValue = false;
	/// <summary>Whether to have an empty response, after <see cref="Tags.Oob"/> tag processing, not change the path used for matching a that pattern.</summary>
	public bool ThatExcludeEmptyResponse = false;

	// These go in their own files.
	/// <summary>Defines default values for bot predicates, used by the <c>bot</c> template element.</summary>
	[JsonIgnore] public Dictionary<string, string> BotProperties { get; private set; } = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
	/// <summary>Defines default values for user predicates, used by the <c>get</c> template element.</summary>
	[JsonIgnore] public Dictionary<string, string> DefaultPredicates { get; private set; } = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
	/// <summary>Defines substitutions used by the <c>gender</c> template element.</summary>
	[JsonIgnore] public SubstitutionList GenderSubstitutions   { get; private set; } = new SubstitutionList();
	/// <summary>Defines substitutions used by the <c>person</c> template element.</summary>
	[JsonIgnore] public SubstitutionList PersonSubstitutions   { get; private set; } = new SubstitutionList();
	/// <summary>Defines substitutions used by the <c>person2</c> template element.</summary>
	[JsonIgnore] public SubstitutionList Person2Substitutions  { get; private set; } = new SubstitutionList();
	/// <summary>Defines substitutions used in the normalisation process.</summary>
	[JsonIgnore] public SubstitutionList NormalSubstitutions   { get; private set; } = new SubstitutionList();
	/// <summary>Defines substitutions used in the denormalisation process.</summary>
	[JsonIgnore] public SubstitutionList DenormalSubstitutions { get; private set; } = new SubstitutionList();

	private void RebuildDictionaries() {
		this.BotProperties = new Dictionary<string, string>(this.BotProperties, this.StringComparer);
		this.DefaultPredicates = new Dictionary<string, string>(this.DefaultPredicates, this.StringComparer);
	}

	public static Config FromFile(string file) {
		if (!File.Exists(file)) return new();
		using var reader = new JsonTextReader(new StreamReader(file));
		return new JsonSerializer().Deserialize<Config>(reader) ?? new();
	}

	private static void Load(string file, object target) {
		using var reader = new JsonTextReader(new StreamReader(file));
		new JsonSerializer().Populate(reader, target);
	}

	public void LoadPredicates(string file) => Load(file, this.BotProperties);
	public void LoadGender(string file) => Load(file, this.GenderSubstitutions);
	public void LoadPerson(string file) => Load(file, this.PersonSubstitutions);
	public void LoadPerson2(string file) => Load(file, this.Person2Substitutions);
	public void LoadNormal(string file) => Load(file, this.NormalSubstitutions);
	public void LoadDenormal(string file) => Load(file, this.DenormalSubstitutions);
	public void LoadDefaultPredicates(string file) => Load(file, this.DefaultPredicates);

	public string GetDefaultPredicate(string name) => this.DefaultPredicates.GetValueOrDefault(name, this.DefaultPredicate);

	public class SubstitutionConverter : JsonConverter {
		public override bool CanConvert(Type type) => type == typeof(Substitution);
		public override bool CanRead => true;
		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
			var list = serializer.Deserialize<string[]>(reader) ?? throw new JsonSerializationException($"{nameof(Substitution)} cannot be null.");
			return list.Length switch {
				2 => new Substitution(list[0], list[1]),
				3 => new Substitution(list[0], list[1], list[2].Equals("regex", StringComparison.OrdinalIgnoreCase)),
				_ => throw new JsonSerializationException($"{nameof(Substitution)} must have exactly 2 or 3 elements.")
			};
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
			if (value == null) {
				writer.WriteNull();
				return;
			}
			var substitution = (Substitution) value;
			writer.WriteStartArray();
			writer.WriteValue(substitution.Pattern);
			writer.WriteValue(substitution.Replacement);
			if (substitution.IsRegex) writer.WriteValue("regex");
			writer.WriteEndArray();
		}
	}
}
