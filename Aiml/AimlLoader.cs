using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Aiml {
	public delegate TemplateNode ElementHandler(XmlNode node, AimlLoader loader);

	public class AimlLoader {
		private Bot bot;

		private static Dictionary<string, ElementHandler> elementHandlers = new Dictionary<string, ElementHandler>(StringComparer.InvariantCultureIgnoreCase) {
			// AIML 1.1 elements
			{ "star"        , TemplateNode.Star.FromXml         },
			{ "that"        , TemplateNode.That.FromXml         },
			{ "input"       , TemplateNode.Input.FromXml        },
			{ "thatstar"    , TemplateNode.ThatStar.FromXml     },
			{ "topicstar"   , TemplateNode.TopicStar.FromXml    },
			{ "get"         , TemplateNode.Get.FromXml          },
			{ "bot"         , TemplateNode.Bot.FromXml          },
			{ "date"        , TemplateNode.Date.FromXml         },
			{ "id"          , TemplateNode.ID.FromXml           },
			{ "size"        , TemplateNode.Size.FromXml         },
			{ "version"     , TemplateNode.Version.FromXml      },
			{ "uppercase"   , TemplateNode.Uppercase.FromXml    },
			{ "lowercase"   , TemplateNode.Lowercase.FromXml    },
			{ "formal"      , TemplateNode.Formal.FromXml       },
			{ "sentence"    , TemplateNode.Sentence.FromXml     },
			{ "condition"   , TemplateNode.Condition.FromXml    },
			{ "random"      , TemplateNode.Random.FromXml       },
			{ "set"         , TemplateNode.Set.FromXml          },
			{ "gossip"      , TemplateNode.Gossip.FromXml       },
			{ "srai"        , TemplateNode.Srai.FromXml         },
			{ "sr"          , TemplateNode.SR.FromXml           },
			{ "person"      , TemplateNode.Person.FromXml       },
			{ "person2"     , TemplateNode.Person2.FromXml      },
			{ "gender"      , TemplateNode.Gender.FromXml       },
			{ "think"       , TemplateNode.Think.FromXml        },
			{ "learn"       , TemplateNode.Learn.FromXml        },
			{ "system"      , TemplateNode.System.FromXml       },
			{ "javascript"  , TemplateNode.JavaScript.FromXml   },

			// AIML 2.0 elements
			{ "loop"        , TemplateNode.Loop.FromXml         },
			{ "sraix"       , TemplateNode.SraiX.FromXml        },
			{ "map"         , TemplateNode.Map.FromXml          },
			{ "explode"     , TemplateNode.Explode.FromXml      },
			{ "normalize"   , TemplateNode.Normalize.FromXml    },
			{ "denormalize" , TemplateNode.Denormalize.FromXml  },
			{ "request"     , TemplateNode.Request.FromXml      },
			{ "response"    , TemplateNode.Response.FromXml     },
			{ "learnf"      , TemplateNode.LearnF.FromXml       },
			{ "vocabulary"  , TemplateNode.Vocabulary.FromXml   },
			{ "program"     , TemplateNode.Program.FromXml      },
			{ "interval"    , TemplateNode.Interval.FromXml     },
			{ "oob"         , (node, loader) => Tags.Oob.FromXml(node, loader) },

			// AIML 2.1 draft rich media tags
			{ "button"      , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback", "url") },
			{ "reply"       , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback") },
			{ "link"        , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "url") },
			{ "image"       , (node, loader) => Tags.Oob.FromXml(node, loader) },
			{ "video"       , (node, loader) => Tags.Oob.FromXml(node, loader) },
			{ "card"        , (node, loader) => Tags.Oob.FromXml(node, loader, "image", "title", "subtitle", "button") },
			{ "carousel"    , (node, loader) => Tags.Oob.FromXml(node, loader, "card") },
			{ "delay"       , (node, loader) => Tags.Oob.FromXml(node, loader) },
			{ "split"       , (node, loader) => Tags.Oob.FromXml(node, loader) },
			{ "list"        , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
			{ "olist"       , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },

			// Program AB extension
			{ "select"      , TemplateNode.Select.FromXml       },
			{ "uniq"        , TemplateNode.Uniq.FromXml         },
			{ "first"       , TemplateNode.First.FromXml        },
			{ "rest"        , TemplateNode.Rest.FromXml         },
			{ "addtriple"   , TemplateNode.AddTriple.FromXml    },
			{ "deletetriple", TemplateNode.DeleteTriple.FromXml },

			// Non-standard extensions
			{ "calculate"   , TemplateNode.Calculate.FromXml    },
			{ "test"        , TemplateNode.Test.FromXml },

			// Invalid template-level tags
			{ "eval"        , NotValidHandler                },
			{ "q"           , NotValidHandler                },
			{ "notq"        , NotValidHandler                },
			{ "vars"        , NotValidHandler                },
			{ "subj"        , NotValidHandler                },
			{ "pred"        , NotValidHandler                },
			{ "obj"         , NotValidHandler                },
			{ "text"        , NotValidHandler                },
			{ "postback"    , NotValidHandler                },
			{ "url"         , NotValidHandler                },
			{ "title"       , NotValidHandler                },
			{ "subtitle"    , NotValidHandler                },
			{ "item"        , NotValidHandler                }
		};
		public static Version AimlVersion => new Version(2, 1);
		public bool ForwardCompatible { get; internal set; }

		public AimlLoader(Bot bot) {
			this.bot = bot;
			this.bot.AimlLoader = this;
		}

		private static TemplateNode NotImplementedHandler(XmlNode node, AimlLoader loader) { throw new NotImplementedException("The " + node.Name + " tag is not implemented."); }
		private static TemplateNode NotValidHandler(XmlNode node, AimlLoader loader) {
			//if (loader.ForwardCompatible) return null;
			throw new XmlException("The " + node.Name + " tag is not valid here.");
		}

		public void LoadAimlFiles() => this.LoadAimlFiles(Path.Combine(this.bot.ConfigDirectory, this.bot.Config.AimlDirectory));
		public void LoadAimlFiles(string path) {
			if (!Directory.Exists(path)) throw new FileNotFoundException("Path not found: " + path, path);

			this.bot.Log(LogLevel.Info, "Loading AIML files from " + path);
			string[] files = Directory.GetFiles(path, "*.aiml");

			foreach (string file in files)
				this.LoadAIML(file);

			this.bot.Log(LogLevel.Info, "Finished loading the AIML files. " + Convert.ToString(this.bot.Size) + " categories in " + files.Length + (files.Length == 1 ? " file" : " files") + " processed.");
		}

		public void LoadAIML(string filename) {
			this.bot.Log(LogLevel.Info, "Processing AIML file: " + filename);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.PreserveWhitespace = true;
			xmlDocument.Load(filename);
			this.LoadAIML(xmlDocument, filename);
		}

		public void LoadAIML(XmlDocument doc, string filename) {
			LoadAIML(bot.Graphmaster, doc.DocumentElement, filename);
		}
		public void LoadAIML(PatternNode target, XmlNode doc, string filename) {
			var versionAttribute = doc.Attributes["version"];
			Version version;
			this.ForwardCompatible = (versionAttribute == null || !Version.TryParse(versionAttribute.Value, out version) || version > AimlVersion);

			XmlNodeList childNodes = doc.ChildNodes;
			foreach (XmlNode xmlNode in childNodes) {
				if (xmlNode.Name == "topic") {
					this.ProcessTopic(target, xmlNode, filename);
				} else if (xmlNode.Name == "category") {
					this.ProcessCategory(target, xmlNode, filename);
				}
			}
			//GC.Collect();
		}

		private void ProcessTopic(PatternNode target, XmlNode node, string filename) {
			string topicName = "*";
			if (node.Attributes.Count == 1 & node.Attributes[0].Name == "name") {
				topicName = node.Attributes["name"].Value;
			}
			foreach (XmlNode xmlNode in node.ChildNodes) {
				if (xmlNode.Name == "category") {
					this.ProcessCategory(target, xmlNode, topicName, filename);
				}
			}
		}

		public void ProcessCategory(PatternNode target, XmlNode node, string filename) {
			this.ProcessCategory(target, node, "*", filename);
		}
		public void ProcessCategory(PatternNode target, XmlNode node, string topicName, string filename) {
			XmlNode? patternNode = null, templateNode = null, thatNode = null, topicNode = null;

			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.Name.Equals("pattern", StringComparison.InvariantCultureIgnoreCase)) patternNode = node2;
				else if (node2.Name.Equals("template", StringComparison.InvariantCultureIgnoreCase)) templateNode = node2;
				else if (node2.Name.Equals("that", StringComparison.InvariantCultureIgnoreCase)) thatNode = node2;
				else if (node2.Name.Equals("topic", StringComparison.InvariantCultureIgnoreCase)) topicNode = node2;
			}
			if (patternNode == null) throw new AimlException("Missing pattern tag in a node found in " + filename + ".");
			if (templateNode == null) throw new AimlException("Node missing a template, with pattern '" + patternNode.InnerXml + "' in file " + filename + ".");
			if (string.IsNullOrWhiteSpace(patternNode.InnerXml)) this.bot.Log(LogLevel.Warning,
				"Attempted to load a new category with an empty pattern, with template '" + templateNode.OuterXml + " in file " + filename + "."
			);

			// Parse the template.
			var templateContent = TemplateElementCollection.FromXml(templateNode, this);

			target.AddChild(this.GeneratePath(patternNode, thatNode, topicNode, topicName, false), new Template(this.bot, templateNode, templateContent, filename));
			++bot.Size;
		}

		[Obsolete]
		public TemplateNode[] ParseTags(XmlNode node) {
			List<TemplateNode> tagList = new List<TemplateNode>();
			foreach (XmlNode node2 in node.ChildNodes) {
				TemplateNode? tag;
				if (node2.NodeType == XmlNodeType.Whitespace)
					tag = new TemplateText(" ");
				else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace)
					tag = new TemplateText(node2.InnerText);
				else if (node2.NodeType == XmlNodeType.Element)
					tag = ParseElement(node2);
				else
					tag = null;

				if (tag != null) tagList.Add(tag);
			}
			return tagList.ToArray();
		}

		public TemplateNode ParseElement(XmlNode node) {
			ElementHandler handler;
			if (elementHandlers.TryGetValue(node.Name, out handler))
				return handler(node, this);

			if (this.ForwardCompatible) return Tags.Oob.FromXml(node, this);
			throw new XmlException($"'{node.Name}' is not a valid AIML {AimlVersion} tag.");
		}

		private IEnumerable<PathToken> GeneratePath(XmlNode patternNode, XmlNode? thatNode, XmlNode? topicNode, string topic, bool isUserInput) {
			var patternTokens = this.ParsePattern(patternNode);
			var thatTokens = this.ParsePattern(thatNode);
			var topicTokens = topicNode != null ? this.ParsePattern(topicNode) :
				topic.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => new PathToken(s, false));

			foreach (var token in patternTokens) yield return token;
			yield return PathToken.ThatSeparator;
			foreach (var token in thatTokens) yield return token;
			yield return PathToken.TopicSeparator;
			foreach (var token in topicTokens) yield return token;
		}

		private IList<PathToken> ParsePattern(XmlNode? xmlNode) {
			if (xmlNode == null) return new[] { new PathToken("*") };

			var tokens = new List<PathToken>();
			foreach (XmlNode xmlNode2 in xmlNode.ChildNodes) {
				if (xmlNode2.NodeType == XmlNodeType.Text) {
					tokens.AddRange(xmlNode2.InnerText.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries)
														.Select(s => new PathToken(s, false)));
				} else if (xmlNode2.NodeType == XmlNodeType.Element) {
					if (xmlNode2.Name.Equals("bot", StringComparison.InvariantCultureIgnoreCase)) {
						// Bot properties are assumed to never change during the bot's uptime, so we process them here.
						string value = this.bot.GetProperty(xmlNode2.Attributes["name"].Value);
						if (value != null) tokens.Add(new PathToken(value, false));
					} else if (xmlNode2.Name.Equals("set", StringComparison.InvariantCultureIgnoreCase)) {
						tokens.Add(new PathToken(xmlNode2.InnerText, true));
					} else {
						throw new XmlException("Unknown pattern tag: " + xmlNode2.Name);
					}
				}
			}
			return tokens;
		}
	}
}
