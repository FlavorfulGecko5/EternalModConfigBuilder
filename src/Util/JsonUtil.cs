using Newtonsoft.Json.Linq;
class JsonUtil
{
    public static bool isOptionList(JToken token)
    {
        switch(token.Type)
        {
            case JTokenType.Array:
                return true;
            
            case JTokenType.Object:
                JToken? objToken = ((JObject)token).GetValue(PROPERTY_VALUE);
                if(objToken == null)
                    return false;
                return objToken.Type == JTokenType.Array;

            default:
                return false;
        }
    }

    public static string[]? readAnyTokenValue(JToken token)
    {
        return read(token, true, true);
    }

    public static string[]? readListTokenValue(JToken list)
    {
        if (list.Type != JTokenType.Array)
            return null;
        return read(list, false, true);
    }

    private static string[]? read(JToken? token, bool allowObjects, bool allowLists)
    {
        if (token == null)
            return null;
        switch (token.Type)
        {
            case JTokenType.Array:
                if(!allowLists)
                    return null;
                int i = 0;
                string[] itemList = new String[token.Count()];
                foreach(JToken element in token)
                {
                    string[]? item = read(element, false, false);
                    if(item == null)
                        return null;
                    itemList[i++] = item[0];
                }
                return itemList;

            case JTokenType.Object:
                if (!allowObjects)
                    return null;
                JToken? objToken = ((JObject)token).GetValue(PROPERTY_VALUE);
                return read(objToken, false, true);

            case JTokenType.Boolean: case JTokenType.String:
            case JTokenType.Integer: case JTokenType.Float:
                return new string[] {token.ToString()};

            default:
                return null;
        }
    }
}