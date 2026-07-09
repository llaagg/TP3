public class Identity : IIdentity
{
    public string Name { get; set; } = string.Empty;
}

public interface IIdentity
{
    string Name { get; set; }
}