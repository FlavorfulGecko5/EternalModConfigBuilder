class Option
{
    public string name { get; }
    public string value {get;}

    public Option(string labelParameter, string valueParameter)
    {
        name = labelParameter;
        value = valueParameter;
    }

    public override string ToString()
    {
        return "Option '" + name + "' - Value: '" + value + "'";
    }
}
