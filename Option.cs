class Option
{
    public string label { get; }
    public string varValue {get;}

    public Option(string labelParameter)
    {
        label = labelParameter;
        varValue = "";
    }

    public Option(string labelParameter, string varValueParameter)
    {
        label = labelParameter;
        varValue = varValueParameter;
    }

    public override string ToString()
    {
        return "Configuration Option of type 'Option'\n"
            + "Label: " + label + "\n";
    }
}
