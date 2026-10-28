namespace CorewoodFortified;

// Ziplines are based on anwo's Ziplines mod, used with permission
internal static class ZiplineMessages
{
	public enum Level
	{
		Short,
		All,
		Off
	}

	// Short shows the brief text in the top-left corner, All the full text where the caller asks
	public static void Say(Player player, string full, string brief, MessageHud.MessageType fullType = MessageHud.MessageType.Center)
	{
		if (player == null || MainConfig.ziplineMessages == null)
		{
			return;
		}
		switch (MainConfig.ziplineMessages.Value)
		{
		case Level.Off:
			break;
		case Level.All:
			if (!string.IsNullOrEmpty(full))
			{
				player.Message(fullType, Localise(full));
			}
			break;
		default:
			if (!string.IsNullOrEmpty(brief))
			{
				player.Message(MessageHud.MessageType.TopLeft, Localise(brief));
			}
			break;
		}
	}

	// Translated here as well as by the message box, so the $KEY_ names inside a translated text become the bound keys
	private static string Localise(string text)
	{
		return Localization.instance != null ? Localization.instance.Localize(text) : text;
	}

	public static void LogInfo(string text)
	{
		Jotunn.Logger.LogInfo("[CorewoodFortified] " + text);
	}

	public static void LogWarning(string text)
	{
		Jotunn.Logger.LogWarning("[CorewoodFortified] " + text);
	}

	public static void LogDebug(string text)
	{
		Jotunn.Logger.LogDebug("[CorewoodFortified] " + text);
	}
}
