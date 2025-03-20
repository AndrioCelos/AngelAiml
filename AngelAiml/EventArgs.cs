using System.ComponentModel;

namespace AngelAiml;
public class GossipEventArgs(string message) : HandledEventArgs {
	public string Message { get; set; } = message;
}

public class PostbackRequestEventArgs(Request request) : EventArgs {
	public Request Request { get; } = request;
}

public class PostbackResponseEventArgs(Response response) : EventArgs {
	public Response Response { get; } = response;
}
