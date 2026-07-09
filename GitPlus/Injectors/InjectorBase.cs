namespace GitPlus.Injectors;

public abstract class InjectorBase
{
    /// <summary>Determines whether this injector applies to a window with the given caption.</summary>
    public abstract bool CanInject(string caption);

    /// <summary>Performs UI injection into the target window.</summary>
    public abstract Task InjectAsync(string caption, CancellationToken cancellationToken = default);
}
