using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using AngelAiml;

namespace AngelAiml.Timers;
public class TimersExtension : IAimlExtension {
	internal readonly List<BotTimer> timers = [];

	public void Initialise() {
		AimlLoader.AddCustomOobHandler("timer", (element, response) => {
			TimeSpan? duration = null; string? name = null; string? postback = null; var repeat = false;
			if (element.Attribute("name") is { } attr) name = attr.Value.Trim();
			if (element.Attribute("duration") is { } attr2) duration = TimeSpan.FromSeconds(double.Parse(attr2.Value));
			if (element.Attribute("postback") is { } attr3) postback = attr3.Value;
			if (element.Attribute("repeat") is { } attr4)
				repeat = attr4.Value.Trim().ToLowerInvariant() is "" or "true" or "yes" || int.TryParse(attr4.Value, out var i) && i != 0;

			var anyNodes = false;
			foreach (var element2 in element.Elements()) {
				anyNodes = true;
				switch (element2.Name.LocalName.ToLowerInvariant()) {
					case "name": name = element2.Value.Trim(); break;
					case "duration": duration = TimeSpan.FromSeconds(double.Parse(element2.Value)); break;
					case "postback": postback = element2.Value; break;
				}
			}
			if (!anyNodes && postback == null) postback = element.Value;
			if (duration == null || postback == null) throw new XmlException("timer element is missing required values");
			var ev = new BotTimer(this, duration.Value, name, repeat, response.User, postback);
			timers.Add(ev);
		});
		AimlLoader.AddCustomOobHandler("stoptimer", (element, response) => {
			var text = element.Value.Trim();
			for (var i = timers.Count - 1; i >= 0; --i) {
#if NET6_0_OR_GREATER
				const char star = '*';
#else
				const string star = "*";
#endif
				if (text != "*" && (timers[i].Name is not null || (text.EndsWith(star) ? !timers[i].Name!.StartsWith(text[..^1]) : timers[i].Name != text)))
					continue;
				timers[i].timer.Stop();
				timers.RemoveAt(i);
			}
		});
	}
}

public class TimestampService : ISraixService {
	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	public string Process(string text, XElement element, RequestProcess process) {
		return text switch {
			"s" => (stopwatch.ElapsedTicks / Stopwatch.Frequency).ToString(),
			"ms" => (stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000)).ToString(),
			"t" => stopwatch.ElapsedTicks.ToString(),
			_ => throw new ArgumentException("Text should be a valid unit.", nameof(text)),
		};
	}
}
