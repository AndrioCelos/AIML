﻿using System.Text.RegularExpressions;

namespace Aiml;
/// <summary>Transforms English words to singular or plural forms based on regular expression substitutions.</summary>
/// <remarks>
///		<para>This class is based on the class with the same name in JBoss DNA, which in turn was inspired by its namesake in Ruby on Rails.</para>
///		<para>JBoss DNA is available under the GNU General Public License, version 2.1.</para>
/// </remarks>
public class Inflector {
	public List<Rule> Plurals { get; } = new List<Rule>();
	public List<Rule> Singulars { get; } = new List<Rule>();
	public HashSet<string> Uncountables { get; }

	public Inflector() : this(StringComparer.CurrentCultureIgnoreCase) { }
	public Inflector(IEqualityComparer<string> comparer) {
		this.Uncountables = new HashSet<string>(comparer);

		this.Plurals.Add(new Rule("$", "s"));
		this.Plurals.Add(new Rule("s$", "s"));
		this.Plurals.Add(new Rule("(ax|test)is$", "$1es"));
		this.Plurals.Add(new Rule("(octop|vir)(?:us|i)$", "$1i"));
		this.Plurals.Add(new Rule("(?:alias|status)$", "$0es"));
		this.Plurals.Add(new Rule("bus$", "$0es"));
		this.Plurals.Add(new Rule("(?:buffal|tomat)o$", "$0es"));
		this.Plurals.Add(new Rule("([ti])(?:um|a)$", "$1a"));
		this.Plurals.Add(new Rule("sis$", "ses"));
		this.Plurals.Add(new Rule("(?:([^f])fe|([lr])f)$", "$1$2ves"));
		this.Plurals.Add(new Rule("hive$", "$0s"));
		this.Plurals.Add(new Rule("([^aeiouy]|qu)y$", "$1ies"));
		this.Plurals.Add(new Rule("(?:x|ch|ss|sh)$", "$0es"));
		this.Plurals.Add(new Rule("(matr|vert|ind)(?:ix|ex)$", "$1ices"));
		this.Plurals.Add(new Rule("([m|l])(?:ouse|ice)$", "$1ice"));
		this.Plurals.Add(new Rule("^ox$", "$0en"));
		this.Plurals.Add(new Rule("quiz$", "$0zes"));
		// Need to check for the following words that are already plural:
		this.Plurals.Add(new Rule("(?:oxen|octopi|viri|aliases|quizzes)$", "$0")); // special rules

		this.Singulars.Add(new Rule("s$", ""));
		this.Singulars.Add(new Rule("(?:s|si|u)s$", "$0")); // '-us' and '-ss' are already singular
		this.Singulars.Add(new Rule("news$", "$0"));
		this.Singulars.Add(new Rule("([ti])a$", "$1um"));
		this.Singulars.Add(new Rule("(analy|ba|diagno|parenthe|progno|synop|the)s[ei]s$", "$1sis"));
		this.Singulars.Add(new Rule("([^f])ves$", "$1fe"));
		this.Singulars.Add(new Rule("(hive)s$", "$1"));
		this.Singulars.Add(new Rule("(tive)s$", "$1"));
		this.Singulars.Add(new Rule("([lr])ves$", "$1f"));
		this.Singulars.Add(new Rule("([^aeiouy]|qu)ies$", "$1y"));
		this.Singulars.Add(new Rule("series$", "$0"));
		this.Singulars.Add(new Rule("(m)ovies$", "$1ovie"));
		this.Singulars.Add(new Rule("(x|ch|ss|sh)es$", "$1"));
		this.Singulars.Add(new Rule("([m|l])ice$", "$1ouse"));
		this.Singulars.Add(new Rule("(bus)es$", "$1"));
		this.Singulars.Add(new Rule("(o)es$", "$1"));
		this.Singulars.Add(new Rule("(shoe)s$", "$1"));
		this.Singulars.Add(new Rule("(cris|test)[ei]s$", "$1is"));
		this.Singulars.Add(new Rule("^(axe)s$", "$1"));  // Ambiguous between 'axe', 'ax' and 'axis', 'axe' was chosen.
		this.Singulars.Add(new Rule("(octop|vir)(?:i|us)$", "$1us"));
		this.Singulars.Add(new Rule("(alias|status)(?:es)?$", "$1"));  // 'alias' and 'status' are already singular, despite ending with 's'.
		this.Singulars.Add(new Rule("(ox)en", "$1"));
		this.Singulars.Add(new Rule("(vert|ind)ices$", "$1ex"));
		this.Singulars.Add(new Rule("(matr)ices$", "$1ix"));
		this.Singulars.Add(new Rule("(quiz)zes$", "$1"));

		this.AddIrregular("person", "people");
		this.AddIrregular("man", "men");
		this.AddIrregular("child", "children");
		this.AddIrregular("sex", "sexes");
		this.AddIrregular("move", "moves");
		this.AddIrregular("stadium", "stadiums");

		this.Uncountables.Add("equipment");
		this.Uncountables.Add("information");
		this.Uncountables.Add("rice");
		this.Uncountables.Add("money");
		this.Uncountables.Add("species");
		this.Uncountables.Add("series");
		this.Uncountables.Add("fish");
		this.Uncountables.Add("sheep");
	}

	public void AddIrregular(string singular, string plural) {
		var singularRemainder = singular[1..];
		var pluralRemainder = plural[1..];

		// Add rules that check whether the word is already in the required form.
		this.Singulars.Add(new Rule(singular + "$", "$0"));
		this.Plurals.Add(new Rule(plural + "$", "$0"));

		// Capturing the first character preserves its case.
		this.Singulars.Add(new Rule("(" + plural[0] + ")" + pluralRemainder + "$", $"$1{singularRemainder}"));
		this.Plurals.Add(new Rule("(" + singular[0] + ")" + singularRemainder + "$", $"$1{pluralRemainder}"));
	}

	public string Singularize(string word) => this.ApplyRules(word, this.Singulars);
	public string Pluralize(string word) => this.ApplyRules(word, this.Plurals);

	private string ApplyRules(string word, List<Rule> rules) {
		if (string.IsNullOrWhiteSpace(word)) return word;
		word = word.Trim();
		if (this.Uncountables.Contains(word)) return word;

		// Apply the rules in reverse order.
		for (var i = rules.Count - 1; i >= 0; --i) {
			var rule = rules[i];
			var result = rule.Apply(word);
			// If there's no match, rule.Apply returns the same instance.
			// If there is a match, and result is a different instance, we return it immediately.
			if (!ReferenceEquals(result, word)) return result;
		}
		return word;
	}

	public class Rule(Regex pattern, string replacement) {
		public Regex Pattern { get; } = pattern;
		public string Replacement { get; } = replacement;

		public Rule(string pattern, string replacement) : this(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), replacement) { }

		public string Apply(string text) => this.Pattern.Replace(text, this.Replacement);

		public override string ToString() => "/" + this.Pattern.ToString().Replace("/", @"\/") + "/" + this.Replacement.Replace("/", @"\/") + "/i";
	}
}
