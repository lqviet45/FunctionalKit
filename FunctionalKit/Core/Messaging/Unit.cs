namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Represents a void type for functional programming
/// </summary>
public readonly struct Unit
{
    public static Unit Value { get; } = new();
    
    public override string ToString() => "()";
}