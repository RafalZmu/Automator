namespace Fixture;

public class Caller
{
    private readonly Dependency _dependency = new();

    public void Use()
    {
        _ = new Dependency();
        _dependency.Run(1);
        _dependency.Run("text");
        void Local() => _dependency.Run(2);
        Local();
        Action callback = () => _dependency.Run(3);
        callback();
        dynamic unknown = _dependency;
        unknown.Missing(4);
    }
}

public class Derived : Dependency { }
