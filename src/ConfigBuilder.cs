using JSONEval.ExpressionEvaluation;
using JSONEval.JSONParsing;
using Newtonsoft.Json.Linq;
/// <summary>
/// Reads and parses JSON configuration files for EternalModBuilder
/// Option data
/// </summary>
class ConfigBuilder
{
    const string PROPERTY_PROPAGATE = "Propagate";
    
    public static List<PropagateList> buildConfig(List<string> configPaths)
    {
        const string RULES_PROPAGATER = "The '" + PROPERTY_PROPAGATE + "' property must obey these rules:\n"
        + "- It must be defined as an object.\n"
        + "- All it's sub-properties must be defined as lists of strings.\n"
        + "- The names and values of these string lists must be formatted according to the rules for Propagation filepaths.\n"
        + "This property is used to control EternalModBuilder's Propagation feature.";

        List<PropagateList> propagations = new List<PropagateList>();
        Parser p = new Parser()
        {
            vars = Evaluator.globalVars
        };

        foreach(string path in configPaths)
        {
            string fileText = File.ReadAllText(path);
            Dictionary<string, JToken> propWrapper;
            try
            {
                propWrapper = p.Parse(fileText, PROPERTY_PROPAGATE);
            }
            catch(JSONEval.JSONParsing.ParserException e)
            {
                throw ConfigErrorNew(e.Message);
            }

            if(propWrapper.Count == 0)
                continue;
            
            JToken propToken = propWrapper[PROPERTY_PROPAGATE];
            if(propToken.Type != JTokenType.Object)
                throw ConfigErrorNew("'" + PROPERTY_PROPAGATE + "' properties must be objects.\n\n" + RULES_PROPAGATER);
            
            foreach(JProperty list in ((JObject)propToken).Properties())
            {
                string[] propPaths = {};
                if(!Parser.ParseStringList(list.Value, ref propPaths, false))
                    throw ConfigErrorNew("All properties in '" + PROPERTY_PROPAGATE + "' must be string lists.\n\n" + RULES_PROPAGATER);
                
                try
                {
                    propagations.Add(new PropagateList(list.Name, propPaths));
                }
                catch(PropagateList.EMBPropagaterListException e)
                {
                    throw ConfigErrorNew(e.Message);
                }
            }

            EMBConfigException ConfigErrorNew(string msg)
            {
                string formattedMessage = String.Format(
                    "Problem encountered in config. file '{0}':\n{1}",
                    path, msg
                );

                return new EMBConfigException(formattedMessage);
            }
        }

        if(EternalModBuilder.runParms.logfile)
        {
            string logMessage = "Parsed Variables:\n" + Evaluator.globalVars.ToString();

            foreach(PropagateList propList in propagations)
                logMessage += propList.ToString() + '\n';
            
            EternalModBuilder.logData.Append(logMessage + "\n\n");
        }
        return propagations;
    }

    public class EMBConfigException : EMBException
    {
        public EMBConfigException(string msg) : base(msg) { }
    }
}