using Amnesia.Core;

namespace Amnesia.World;

/// A* scritto a mano sulla maschera di calpestabilita' della mappa.
///
/// La proprieta' che conta non e' la lunghezza del percorso — ce ne sono molti
/// ugualmente corti fra due celle — ma quale dei tanti viene scelto: la cella
/// successiva di un percorso minimo deve essere funzione pura della cella su cui
/// si sta. Il sistema di movimento cammina una cella per volta richiedendo il
/// percorso ogni volta, e quella e' l'unica cosa che rende la stessa camminata
/// indipendente da come il tempo viene affettato.
///
/// Percio' i pareggi si rompono su un ordine totale fisso — f piu' basso, poi
/// riga piu' bassa, poi colonna piu' bassa — e mai sull'ordine di inserimento in
/// una coda o sull'ordine di enumerazione di una tabella hash, che dipendono da
/// quante volte si e' chiamato e da un seme che cambia a ogni processo.
public sealed class Navigator
{
    /// Niente diagonali: una cella di cammino e' una cella di cammino in ogni
    /// direzione, cosi' il costo in minuti di un percorso e' la sua lunghezza.
    /// L'ordine e' fisso, e vale come ultimo criterio quando due vie arrivano
    /// sulla stessa cella con lo stesso costo.
    private static readonly Cell[] Steps =
    {
        new(0, -1),
        new(-1, 0),
        new(1, 0),
        new(0, 1),
    };

    private readonly int _width;
    private readonly int _height;
    private readonly bool[] _solid;

    public Navigator(VillageMap map)
    {
        _width = Math.Max(map.Width, 0);
        _height = Math.Max(map.Height, 0);
        _solid = new bool[_width * _height];
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _solid[(y * _width) + x] = !map.IsWalkable(new Cell(x, y));
            }
        }
    }

    public bool InRegion(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < _width && cell.Y < _height;

    public bool IsSolid(Cell cell) => !InRegion(cell) || _solid[(cell.Y * _width) + cell.X];

    /// Il percorso da una cella all'altra, esclusa quella di partenza e inclusa
    /// quella di arrivo. Vuoto se non c'e' via, se si e' gia' arrivati, o se una
    /// delle due celle e' un muro o sta fuori dalla mappa.
    public IReadOnlyList<Cell> Path(Cell from, Cell to)
    {
        if (from == to || !InRegion(from) || !InRegion(to) || IsSolid(from) || IsSolid(to))
        {
            return Array.Empty<Cell>();
        }

        var count = _width * _height;
        var cost = new int[count];
        var previous = new int[count];
        var closed = new bool[count];
        for (var index = 0; index < count; index++)
        {
            cost[index] = int.MaxValue;
            previous[index] = -1;
        }

        var start = Index(from);
        var goal = Index(to);
        cost[start] = 0;

        // La frontiera ordinata per (f, y, x). Un elemento vecchio resta dentro
        // quando lo stesso nodo viene raggiunto meglio: costa meno saltarlo
        // all'estrazione che cercarlo per toglierlo, e l'ordine non cambia.
        var frontier = new SortedSet<(int F, int Y, int X)>
        {
            (Heuristic(from, to), from.Y, from.X),
        };

        while (frontier.Count > 0)
        {
            var best = frontier.Min;
            frontier.Remove(best);
            var current = new Cell(best.X, best.Y);
            var currentIndex = Index(current);
            if (closed[currentIndex])
            {
                continue;
            }
            closed[currentIndex] = true;
            if (currentIndex == goal)
            {
                break;
            }

            foreach (var step in Steps)
            {
                var next = new Cell(current.X + step.X, current.Y + step.Y);
                if (!InRegion(next) || IsSolid(next))
                {
                    continue;
                }
                var nextIndex = Index(next);
                if (closed[nextIndex])
                {
                    continue;
                }
                var candidate = cost[currentIndex] + 1;
                // A parita' di costo si tiene il primo arrivato: l'euristica e'
                // consistente e l'ordine di estrazione e' totale, quindi "il
                // primo" e' sempre lo stesso primo.
                if (candidate >= cost[nextIndex])
                {
                    continue;
                }
                cost[nextIndex] = candidate;
                previous[nextIndex] = currentIndex;
                frontier.Add((candidate + Heuristic(next, to), next.Y, next.X));
            }
        }

        if (previous[goal] < 0)
        {
            return Array.Empty<Cell>();
        }

        var route = new List<Cell>();
        for (var index = goal; index != start && index >= 0; index = previous[index])
        {
            route.Add(new Cell(index % _width, index / _width));
        }
        route.Reverse();
        return route;
    }

    /// Stare dove si sta gia' conta come raggiungibile, ma solo se ci si puo'
    /// stare: un muro o una cella fuori mappa non e' raggiungibile mai, nemmeno
    /// da se stessa.
    public bool Reachable(Cell from, Cell to)
    {
        if (Path(from, to).Count > 0)
        {
            return true;
        }
        return from == to && InRegion(from) && !IsSolid(from);
    }

    private int Index(Cell cell) => (cell.Y * _width) + cell.X;

    private static int Heuristic(Cell from, Cell to) =>
        Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
}
