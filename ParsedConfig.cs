class ParsedConfig
{
    private List<string> declFiles {get;}
    private List<Option> configOptions {get;}

    public ParsedConfig(List<string> declFilesParameter, List<Option> configOptionsParameter)
    {
        declFiles = declFilesParameter;
        configOptions = configOptionsParameter;
    }
}