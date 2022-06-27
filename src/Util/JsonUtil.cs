using Newtonsoft.Json.Linq;
class JsonUtil
{
    public static string[]? readList(JToken list)
    {
        if (list.Type != JTokenType.Array)
            return null;
        
        int i = 0;
        string[] filePaths = new String[list.Count()];
        foreach(JToken element in list)
        {
            string? path = readTokenValue(element, false);
            if (path == null)
                return null;
            filePaths[i++] = path;
        }
        return filePaths;
    }

    public static string? readTokenValue(JToken? token, bool allowObjects)
    {
        if (token == null)
            return null;
        switch (token.Type)
        {
            case JTokenType.Object:
                if (!allowObjects)
                    return null;
                JToken? objToken = ((JObject)token).GetValue(PROPERTY_VALUE);
                return readTokenValue(objToken, false);

            case JTokenType.Boolean: case JTokenType.String:
            case JTokenType.Integer: case JTokenType.Float:
                return token.ToString();

            default:
                return null;
        }
    }
}