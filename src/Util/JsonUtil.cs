using Newtonsoft.Json.Linq;
class JsonUtil
{
    public static string[]? readAnyTokenValue(JToken token)
    {
        return read(token, true, true);
    }

    public static string[]? readListTokenValue(JToken list)
    {
        if (list.Type != JTokenType.Array)
            return null;
        string[]? rawList = read(list, false, true);
        
        if(rawList == null)
            return null;
        
        // Since we know we're reading a list, we remove the length element
        string[] elementsOnly = new string[rawList.Length - 1];
        for(int i = 1; i < rawList.Length; i++)
            elementsOnly[i - 1] = rawList[i];
        return elementsOnly;
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

                string[] itemList = new String[token.Count() + 1];
                itemList[0] = token.Count().ToString();
                int i = 1;
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