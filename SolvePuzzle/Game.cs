using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace SolvePuzzle
{
    public class Game
    {
        public Game() { }
        public int CreateMap()
        {
            Trace.WriteLine("Generating map... ");
            Trace.WriteLine($"Map's size: {MapSize}x{MapSize}");
            GenerateMap();
            Trace.WriteLine("Done");
            mOriginMap = new int[TotalPieces];
            Array.Copy(Map, 0, mOriginMap, 0, TotalPieces);
            return 0;
        }
        public int Destroy() { return 0; }

        private int[] GenerateMap()
        {
            BlankPiece = 0;
            Map = new int[TotalPieces];
            List<int> values = new List<int>(Map.Length);
            for (int i = 0; i < Map.Length; ++i) values.Add(i);

            for (int i = 0; i < Map.Length; ++i)
            {
                int index = mGenerator.Next(0, values.Count);
                Map[i] = values[index];
                values.RemoveAt(index);
                // BlankIndex is where the last piece locates, that is mMap[BlankIndex] = size - 1
                if (Map[i] == Map.Length - 1) BlankPiece = i;
            }
            while (!IsSolvable(Map, BlankPiece))
            {
                for (int i = 0; i < Map.Length; ++i) values.Add(i);
                for (int i = 0; i < Map.Length; ++i)
                {
                    int index = mGenerator.Next(0, values.Count);
                    Map[i] = values[index];
                    values.RemoveAt(index);
                    // BlankIndex is where the last piece locates, that is mMap[BlankIndex] = size - 1
                    if (Map[i] == Map.Length - 1) BlankPiece = i;
                }
            }
            return Map;
        }
        static private bool IsSolvable(int[] map, int blankPos)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (blankPos < 0) throw new ArgumentOutOfRangeException(nameof(blankPos));
            if (map.Length <= 2) return true;

            /* --- According to GeekForGeeks https://www.geeksforgeeks.org/check-instance-15-puzzle-solvable/
             * In general, for a given grid of width N, we can find out check if a N*N – 1 puzzle is solvable or not by following below simple rules :
             * If N is odd, then puzzle instance is solvable if number of inversions is even in the input state.
             * If N is even, puzzle instance is solvable if
                 * the blank is on an even row counting from the bottom (second-last, fourth-last, etc.) and number of inversions is odd.
                 * the blank is on an odd row counting from the bottom (last, third-last, fifth-last, etc.) and number of inversions is even.
             * For all other cases, the puzzle instance is not solvable.
            */

            int inversion = 0;
            for (int i = 0; i < map.Length - 1; ++i)
            {
                if (map[i] == map.Length - 1) continue;
                for (int j = i + 1; j < map.Length; ++j)
                {
                    if (map[j] == map.Length - 1) continue;
                    if (map[i] > map[j]) ++inversion;
                }
            }
            if (map.Length % 2 == 1 && inversion % 2 == 0) return true;
            if (map.Length % 2 == 0)
            {
                int mapSize = Convert.ToInt32(Math.Sqrt(map.Length));
                int blankY = blankPos / mapSize;
                int fromBottomBlankY = mapSize - 1 - blankY;
                if (inversion % 2 == 1 && fromBottomBlankY % 2 == 1) return true;
                if (inversion % 2 == 0 && fromBottomBlankY % 2 == 0) return true;
            }
            return false;
        }
        public void Restart()
        {
            Trace.WriteLine("Restarting...");
            TotalMove = 0;
            Array.Copy(mOriginMap, 0, Map, 0, TotalPieces);
            Trace.WriteLine("Finished.");
        }
        public bool IsSolved()
        {
            if (Map == null) throw new NullReferenceException(nameof(Map));
            if (Map[Map.Length - 1] != Map.Length - 1) return false;
            for (int i = 0; i < Map.Length; ++i)
            {
                if (i != Map[i]) return false;
            }
            return true;

        }
        public int Update(int start, int dest)
        {
            if (Map == null) throw new NullReferenceException(nameof(Map));
            int size = TotalPieces;
            if (dest < 0 || dest >= size) return INVALID_MOVE;
            if (start < 0 || start >= size) return INVALID_MOVE;
            if (Map[start] != size - 1) return INVALID_MOVE;

            bool isSameRow = start / MapSize == dest / MapSize;
            bool isSameCol = start % MapSize == dest % MapSize;
            int offset = Math.Abs(start - dest);
            if (offset != MapSize && offset != 1) return INVALID_MOVE;
            if (offset == MapSize && !isSameCol) return INVALID_MOVE;
            if (offset == 1 && !isSameRow) return INVALID_MOVE;

            Map[start] = Map[dest];
            Map[dest] = size - 1;
            BlankPiece = dest;
            Trace.WriteLine($"Move {++TotalMove}: [{start}] -> [{dest}]");
            return 0;
        }
        public void Save(string path, string currentImage)
        {
            if (string.IsNullOrEmpty(path)) throw new NullReferenceException(nameof(path));
            using (StreamWriter writer = File.CreateText(path))
            {
                writer.WriteLine(MapSize);
                writer.WriteLine(TotalMove);
                for (int i = 0; i < TotalPieces; ++i)
                {
                    writer.Write($"{Map[i]},");
                }
                writer.WriteLine("");
                for (int i = 0; i < TotalPieces; ++i)
                {
                    writer.Write($"{mOriginMap[i]},");
                }
                writer.WriteLine("");
                writer.WriteLine(currentImage);
            }
        }
        public bool Load(string path, out string currentImage)
        {
            currentImage = "";
            if (string.IsNullOrEmpty(path)) throw new NullReferenceException(nameof(path));
            if (!File.Exists(path)) return false;

            int size, moves;
            int[] map, origin;
            string current;
            using (StreamReader reader = File.OpenText(path))
            {
                try
                {
                    if (!int.TryParse(reader.ReadLine(), out size)) return false;
                    if (!int.TryParse(reader.ReadLine(), out moves)) return false;

                    string mapStr = reader.ReadLine();
                    string[] tokens = mapStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int total = size * size;
                    map = new int[total];
                    for (int i = 0; i < total; ++i)
                    {
                        if (int.TryParse(tokens[i], out int temp)) map[i] = temp;
                        else return false;
                    }

                    mapStr = reader.ReadLine();
                    tokens = mapStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    origin = new int[total];
                    for (int i = 0; i < total; ++i)
                    {
                        if (int.TryParse(tokens[i], out int temp)) origin[i] = temp;
                        else return false;
                    }
                    current = reader.ReadLine();

                    TotalMove = moves;
                    MapSize = size;
                    Map = map;
                    mOriginMap = origin;
                    currentImage = current;
                    Trace.WriteLine($"TotalMove={TotalMove}, MapSize={MapSize}");
                    Trace.WriteLine($"Current={currentImage}");
                    Trace.Write($"Map: ");
                    for (int i = 0; i < total; ++i)
                    {
                        Trace.Write($"{Map[i]} ");
                    }
                    Trace.WriteLine("");
                    Trace.Write($"OriginMap: ");
                    for (int i = 0; i < total; ++i)
                    {
                        Trace.Write($"{Map[i]} ");
                    }
                    Trace.WriteLine("");
                }
                catch (Exception) { return false; }

            }
            return true;
        }
        private int[] GetMoveInfo(int move)
        {
            return new int[2] { move >> 4, move & 0x11 };
        }
        private int GetMoveValue(int start, int dest)
        {
            return (start << 4) + (dest & 0x11);
        }

        public int BlankPiece { get; private set; }
        public int TotalMove { get; private set; }
        public int MapSize { get; set; }
        public int[] Map { get; private set; }
        private int[] mOriginMap;

        public int TotalPieces => MapSize * MapSize;
        private readonly Random mGenerator = new Random();
        public const int INVALID_MOVE = -1;
    }
}
