using System.Timers;
using AngelAiml;
using Timer = System.Timers.Timer;

namespace AngelAiml.Timers;

public class BotTimer {
	public string? Name { get; }
	private readonly TimersExtension origin;
	public Timer timer;
	public User user;
	public string postback;

	public BotTimer(TimersExtension origin, TimeSpan duration, string? name, bool repeat, User user, string postback) {
		if (string.IsNullOrEmpty(postback)) throw new ArgumentException($"'{nameof(postback)}' cannot be null or empty.", nameof(postback));
		this.origin = origin ?? throw new ArgumentNullException(nameof(origin));
		Name = name;
		timer = new Timer(duration.TotalMilliseconds) { AutoReset = repeat };
		timer.Elapsed += Timer_Elapsed;
		timer.Start();
		this.user = user;
		this.postback = postback;
	}

	private void Timer_Elapsed(object? sender, ElapsedEventArgs e) {
		user.Postback("OOB TICK " + postback);
		if (!timer.AutoReset)
			origin.timers.Remove(this);
	}
}
