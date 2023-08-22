using System.Runtime.CompilerServices;

namespace Aiml;
internal static class Polyfills {
#if !(NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
		if (!dictionary.ContainsKey(key)) {
			dictionary.Add(key, value);
			return true;
		}
		return false;
	}

	public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		=> dictionary.TryGetValue(key, out var value) ? value : defaultValue;
#endif

	public static bool IsClauseVariable(this string value)
#if NET5_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
		=> value.StartsWith('?');
#else
		=> value.Length > 0 && value[0] == '?';
#endif
}
