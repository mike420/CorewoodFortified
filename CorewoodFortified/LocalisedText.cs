namespace CorewoodFortified;

internal class LocalisedText
{
    // One translated text for text built in code, with its $1, $2... filled in with the values given
    public static string Get(string key, params string[] values)
    {
        global::Localization game = global::Localization.instance;
        return game != null ? game.Localize("$" + key, values) : key;
    }
}
