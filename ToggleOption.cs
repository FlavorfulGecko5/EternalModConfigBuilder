class ToggleOption : Option
{
    public bool state { get; }

    public ToggleOption(string labelParameter, bool stateParameter) : base(labelParameter)
    {
        state = stateParameter;
    }

    public override string ToString()
    {
        return "Configuration Option of type 'ToggleOption'\n"
            + "Label: " + label + "\n"
            + "State: " + state + "\n";
    }
}
