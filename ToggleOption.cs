class ToggleOption : Option
{
    public bool value { get; }

    public ToggleOption(string labelParameter, bool stateParameter) : base(labelParameter)
    {
        value = stateParameter;
    }

    public override string ToString()
    {
        return "Configuration Option of type 'ToggleOption'\n"
            + "Label: " + label + "\n"
            + "State: " + value + "\n";
    }
}
