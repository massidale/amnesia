namespace Amnesia.Core;

/// Dove sta un attore. In GDScript era un dizionario senza tipo dentro un altro
/// dizionario senza tipo; qui e' un record, e una cella e' un'astrazione di
/// navigazione, mai di disegno — la presentazione la moltiplica per la sua scala.
public sealed class Actor
{
    public Cell? Position { get; set; }
}

public readonly record struct Cell(int X, int Y)
{
    public override string ToString() => $"({X}, {Y})";
}
