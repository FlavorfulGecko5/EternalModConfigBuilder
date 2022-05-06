class VariableOption : Option
{
    public string value {get;}

    public VariableOption(string labelParameter, string valueParameter) : base(labelParameter)
    {
        value = valueParameter;
    }

    public override string ToString()
    {
        return "Configuration Option of type 'StringOption'\n"
            + "Label: " + label + "\n"
            + "Value: " + value + "\n";
    }
}