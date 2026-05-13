namespace RinhaApi.Services;

/// <summary>
/// Loads preprocessed vector data from a binary file.
/// </summary>
public class PreprocessedDataLoader
{
    public class PreprocessedData
    {
        public required byte[] Vectors { get; set; }
        public required byte[] Labels { get; set; }
        public required Dictionary<long, List<int>> Grid { get; set; }
        public required int VectorSize { get; set; }
        public required int BitsPerDim { get; set; }
        public required int Count { get; set; }
    }

    public static PreprocessedData Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Preprocessed data file not found: {path}");
        }

        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);

        // Read header
        var magic = reader.ReadString();
        if (magic != "RINHA")
        {
            throw new InvalidOperationException("Invalid preprocessed data file format");
        }

        var version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidOperationException($"Unsupported preprocessed data version: {version}");
        }

        var vectorSize = reader.ReadInt32();
        var bitsPerDim = reader.ReadInt32();
        var count = reader.ReadInt32();

        // Read vectors
        var vectorsLength = reader.ReadInt32();
        var vectors = reader.ReadBytes(vectorsLength);

        // Read labels
        var labelsLength = reader.ReadInt32();
        var labels = reader.ReadBytes(labelsLength);

        // Read grid
        var gridCount = reader.ReadInt32();
        var grid = new Dictionary<long, List<int>>(gridCount);

        for (int i = 0; i < gridCount; i++)
        {
            var key = reader.ReadInt64();
            var bucketSize = reader.ReadInt32();
            var bucket = new List<int>(bucketSize);

            for (int j = 0; j < bucketSize; j++)
            {
                bucket.Add(reader.ReadInt32());
            }

            grid[key] = bucket;
        }

        return new PreprocessedData
        {
            Vectors = vectors,
            Labels = labels,
            Grid = grid,
            VectorSize = vectorSize,
            BitsPerDim = bitsPerDim,
            Count = count
        };
    }
}
