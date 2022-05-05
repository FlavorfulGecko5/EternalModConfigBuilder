class Option
{
    public string label { get; }

    public Option(string labelParameter)
    {
        label = labelParameter;
    }

    public override string ToString()
    {
        return "Configuration Option of type 'Option'\n"
            + "Label: " + label + "\n";
    }
}
