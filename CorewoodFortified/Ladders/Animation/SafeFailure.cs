using System;

namespace CorewoodFortified.LadderAnimation;

internal static class SafeFailure
{
	public static string Brief(Exception? exception)
	{
		if (exception == null)
		{
			return "";
		}
		return string.IsNullOrEmpty(exception.Message) ? exception.GetType().Name : exception.GetType().Name + ": " + exception.Message;
	}
}
