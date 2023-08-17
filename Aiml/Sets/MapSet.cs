using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aiml.Sets; 
/// <summary>
///     Represents the set of phrases that are keys in an AIML map.
/// </summary>
public class MapSet : Set {
	public Map Map { get; }
	public Bot Bot { get; }
	public override int MaxWords { get; }

	public MapSet(string mapName, Bot bot) {
		this.Bot = bot ?? throw new ArgumentNullException(nameof(bot));
		this.Map = this.Bot.Maps[mapName];
		this.MaxWords = this.Map is Maps.StringMap stringMap
			? stringMap.dictionary.Keys.Max(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length)
			: int.MaxValue;
	}

	public override bool Contains(string phrase) => this.Map is Maps.StringMap stringMap
		? stringMap[phrase] != null
		: this.Map[phrase] is string s && s != this.Bot.Config.DefaultMap;
}
