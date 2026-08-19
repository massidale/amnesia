using System.Text.Json;
using Amnesia.Core;

namespace Amnesia.World;

/// Un luogo con un nome e' un rettangolo di celle: una stanza, una strada, una
/// piazza. Il gioco ragiona per luoghi, la navigazione per celle.
public readonly record struct PlaceRect(int X, int Y, int W, int H);

/// Il paese come dato, non come scena. Calpestabilita' e luoghi con un nome
/// vivono qui, cosi' un test puo' chiedere dove sta qualcuno senza aprire una
/// finestra; la vista e' solo un disegno di questo.
public sealed class VillageMap
{
    public const string DefaultPath = "content/village_map.json";

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<string> Rows { get; }
    public IReadOnlyDictionary<char, bool> Legend { get; }
    public IReadOnlyDictionary<string, PlaceRect> Places { get; }
    public IReadOnlyDictionary<string, Cell> Spawns { get; }

    /// I luoghi nell'ordine in cui il file li dichiara. Due rettangoli possono
    /// sovrapporsi, e allora la cella appartiene al primo dichiarato: l'ordine
    /// di enumerazione di un dizionario non e' garantito da nessuna parte, e una
    /// mappa che risponde diversamente a seconda della build non e' una mappa.
    private readonly IReadOnlyList<string> _placeOrder;

    private VillageMap(
        int width,
        int height,
        IReadOnlyList<string> rows,
        IReadOnlyDictionary<char, bool> legend,
        IReadOnlyDictionary<string, PlaceRect> places,
        IReadOnlyList<string> placeOrder,
        IReadOnlyDictionary<string, Cell> spawns)
    {
        Width = width;
        Height = height;
        Rows = rows;
        Legend = legend;
        Places = places;
        _placeOrder = placeOrder;
        Spawns = spawns;
    }

    public static Result<VillageMap> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<VillageMap>.Fail("map_not_found", $"mappa del paese non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<VillageMap> FromJson(string json)
    {
        MapDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<MapDto>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<VillageMap>.Fail("invalid_map", "mappa del paese illeggibile");
        }
        if (dto is null)
        {
            return Result<VillageMap>.Fail("invalid_map", "mappa del paese illeggibile");
        }

        var legend = new Dictionary<char, bool>();
        foreach (var entry in dto.Legend)
        {
            // Una voce di legenda e' un carattere disegnato sulla mappa. Se ne
            // vale piu' di uno non e' una legenda con un refuso, e' un formato
            // diverso, e caricarla a meta' nasconderebbe il problema fino a
            // quando qualcuno cammina dentro un muro.
            if (entry.Key.Length != 1)
            {
                return Result<VillageMap>.Fail("invalid_legend", $"voce di legenda non e' un carattere: {entry.Key}");
            }
            legend[entry.Key[0]] = entry.Value;
        }

        var places = new Dictionary<string, PlaceRect>();
        var order = new List<string>();
        foreach (var entry in dto.Places)
        {
            places[entry.Key] = new PlaceRect(entry.Value.X, entry.Value.Y, entry.Value.W, entry.Value.H);
            order.Add(entry.Key);
        }

        var spawns = new Dictionary<string, Cell>();
        foreach (var entry in dto.Spawn)
        {
            spawns[entry.Key] = new Cell(entry.Value.X, entry.Value.Y);
        }

        return Result<VillageMap>.Ok(new VillageMap(
            dto.Width, dto.Height, dto.Rows, legend, places, order, spawns));
    }

    /// Il carattere disegnato su una cella, o niente se la cella e' fuori dalla
    /// mappa o oltre la fine di una riga piu' corta del dovuto.
    public char? Terrain(Cell cell)
    {
        if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height || cell.Y >= Rows.Count)
        {
            return null;
        }
        var row = Rows[cell.Y];
        if (cell.X >= row.Length)
        {
            return null;
        }
        return row[cell.X];
    }

    public bool IsWalkable(Cell cell) =>
        Terrain(cell) is char terrain && Legend.TryGetValue(terrain, out var walkable) && walkable;

    public bool HasPlace(string place) => Places.ContainsKey(place);

    public string PlaceAt(Cell cell)
    {
        foreach (var name in _placeOrder)
        {
            var rect = Places[name];
            if (cell.X >= rect.X && cell.Y >= rect.Y && cell.X < rect.X + rect.W && cell.Y < rect.Y + rect.H)
            {
                return name;
            }
        }
        return "";
    }

    public IReadOnlyList<Cell> CellsOf(string place)
    {
        if (!Places.TryGetValue(place, out var rect))
        {
            return Array.Empty<Cell>();
        }
        var cells = new List<Cell>(Math.Max(rect.W * rect.H, 0));
        for (var dy = 0; dy < rect.H; dy++)
        {
            for (var dx = 0; dx < rect.W; dx++)
            {
                cells.Add(new Cell(rect.X + dx, rect.Y + dy));
            }
        }
        return cells;
    }

    /// Niente per un luogo che non esiste. In GDScript rispondeva (0, 0), che e'
    /// una cella vera: un refuso in un file di contenuto diventava un personaggio
    /// che non si muove piu', e nessuno sapeva perche'.
    public Cell? CenterOf(string place) =>
        Places.TryGetValue(place, out var rect)
            ? new Cell(rect.X + rect.W / 2, rect.Y + rect.H / 2)
            : null;

    public Cell? Spawn(string actorId) =>
        Spawns.TryGetValue(actorId, out var cell) ? cell : null;

    private sealed class MapDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Rows { get; set; } = new();
        public Dictionary<string, bool> Legend { get; set; } = new();
        public Dictionary<string, RectDto> Places { get; set; } = new();
        public Dictionary<string, CellDto> Spawn { get; set; } = new();
    }

    private sealed class RectDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    private sealed class CellDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}

/// Il formato dei file di contenuto e' minuscolo, il C# no: la corrispondenza
/// senza maiuscole e' l'unica cosa che li tiene insieme.
internal static class Dto
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
