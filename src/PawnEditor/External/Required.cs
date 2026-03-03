namespace System.Runtime.CompilerServices;

public sealed class RequiredMemberAttribute : Attribute
{
}

public sealed class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name)
    {
    }
}