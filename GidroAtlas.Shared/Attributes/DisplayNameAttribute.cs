namespace GidroAtlas.Shared.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class DisplayNameAttribute : Attribute
{
    public string Name { get; }

    public DisplayNameAttribute(string name)
    {
        Name = name;
    }
}
