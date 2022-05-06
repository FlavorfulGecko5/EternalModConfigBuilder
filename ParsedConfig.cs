class ParsedConfig
{
    private bool mustCheckAllDecls;
    private List<string> declFiles {get;}
    private List<Option> configOptions {get;}

    public ParsedConfig(List<string> declFilesParameter, List<Option> configOptionsParameter, bool mustCheckAllDeclsParameter)
    {
        declFiles = declFilesParameter;
        configOptions = configOptionsParameter;
        mustCheckAllDecls = mustCheckAllDeclsParameter;
    }
}