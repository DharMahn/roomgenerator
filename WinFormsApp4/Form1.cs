using System.Diagnostics;
using System.Drawing;
using static WinFormsApp4.Room;

namespace WinFormsApp4
{
    public partial class Form1 : Form
    {
        public Room room;

        private static Form1 _instance = null;
        public static Form1 Instance => _instance ??= new Form1();
        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }
        protected override void OnShown(EventArgs e)
        {
            RoomGenerator gen = new(100, 100);
            BackgroundImageLayout = ImageLayout.Stretch;
            room = gen.GenerateRoom();
            BackgroundImage = room.GenerateBitmap();
        }
    }
    public static class ConnectivityExtensions
    {
        public static IEnumerable<Connectivity> GetIndividualFlags(Connectivity connectivity)
        {
            foreach (Connectivity flag in Enum.GetValues(typeof(Connectivity)))
            {
                if (flag == Connectivity.None || flag == Connectivity.All)
                {
                    continue; // Skip the 'None' value
                }

                if (connectivity.HasFlag(flag))
                {
                    yield return flag;
                }
            }
        }
    }

    public class RoomGenerator
    {
        private int roomWidth, roomHeight;
        private MetaTile[,] roomLayout;
        private Queue<(int x, int y, Connectivity directionFrom)> expansionQueue = new Queue<(int x, int y, Connectivity directionFrom)>();
        private Random rng = new Random();

        public RoomGenerator(int roomWidth, int roomHeight)
        {
            this.roomWidth = roomWidth;
            this.roomHeight = roomHeight;
            this.roomLayout = new MetaTile[roomWidth, roomHeight];
        }

        public Room GenerateRoom()
        {
            // Initialize with a starting MetaTile that opens in at least one direction
            int startX = rng.Next(roomWidth);
            int startY = rng.Next(roomHeight);
            Connectivity startDirection = (Connectivity)rng.Next(1, 16); // Exclude None, include All

            roomLayout[startX, startY] = MetaTile.GenerateMetaTile(startDirection);
            Debug.WriteLine($"Started at: {startX};{startY} with direction {startDirection}");

            // Add open directions to the queue for the starting tile
            AddToQueue(startX, startY, startDirection);

            while (expansionQueue.Count > 0)
            {
                var (x, y, directionFrom) = expansionQueue.Dequeue();
                TryExpand(x, y, directionFrom);
            }

            // Fill the rest with closed tiles
            FillRemainingWithClosedTiles();

            return new Room(roomLayout);
        }

        private void AddToQueue(int x, int y, Connectivity direction)
        {
            // This method now adds all possible expansion directions to the queue based on the current tile's connectivity
            foreach (var directionFlag in ConnectivityExtensions.GetIndividualFlags(direction))
            {
                var (nextX, nextY) = GetNextCoordinates(x, y, directionFlag);
                if (IsInBounds(nextX, nextY) && roomLayout[nextX, nextY] == null)
                {
                    expansionQueue.Enqueue((nextX, nextY, GetOppositeDirection(directionFlag)));
                }
            }
        }
        private static Connectivity GetOppositeDirection(Connectivity direction)
        {
            switch (direction)
            {
                case Connectivity.Left: return Connectivity.Right;
                case Connectivity.Right: return Connectivity.Left;
                case Connectivity.Top: return Connectivity.Bottom;
                case Connectivity.Bottom: return Connectivity.Top;
                default: return Connectivity.None;
            }
        }
        private void TryExpand(int x, int y, Connectivity directionFrom)
        {
            if (!IsInBounds(x, y) || roomLayout[x, y] != null) return;

            // Generate the new tile with at least the opening back to the tile we're expanding from
            Connectivity newTileConnectivity = directionFrom | GetRandomConnectivityExcluding(directionFrom);

            roomLayout[x, y] = MetaTile.GenerateMetaTile(newTileConnectivity);
            Debug.WriteLine($"Expanding at: {x};{y} with connectivity: {newTileConnectivity}");

            AddToQueue(x, y, newTileConnectivity);
        }

        private static (int, int) GetNextCoordinates(int x, int y, Connectivity direction)
        {
            switch (direction)
            {
                case Connectivity.Left: return (x - 1, y);
                case Connectivity.Right: return (x + 1, y);
                case Connectivity.Top: return (x, y - 1);
                case Connectivity.Bottom: return (x, y + 1);
                default: return (x, y); // Should never happen
            }
        }

        private bool IsInBounds(int x, int y) => x >= 0 && x < roomWidth && y >= 0 && y < roomHeight;

        private Connectivity GetRandomConnectivityExcluding(Connectivity exclude)
        {
            // Generate a random connectivity that does not include 'exclude'. This ensures variety in connectivity.
            Connectivity result;
            do
            {
                result = (Connectivity)rng.Next(1, 16);
            } while (result == exclude || !ConnectivityExtensions.GetIndividualFlags(result).Contains(exclude));
            return result;
        }

        private void FillRemainingWithClosedTiles()
        {
            for (int x = 0; x < roomWidth; x++)
            {
                for (int y = 0; y < roomHeight; y++)
                {
                    if (roomLayout[x, y] == null)
                    {
                        roomLayout[x, y] = MetaTile.GenerateMetaTile(Connectivity.None); // Closed tile
                    }
                }
            }
        }
    }

    public class Room
    {
        MetaTile[,] tiles;
        public Room(int roomWidth, int roomHeight)
        {
            tiles = new MetaTile[roomWidth, roomHeight];

        }
        public Room(MetaTile[,] tiles) => this.tiles = tiles;

        public MetaTile this[int x, int y]
        {
            get => tiles[x, y];
            set => tiles[x, y] = value;
        }
        public Bitmap GenerateBitmap()
        {
            int roomWidth = tiles.GetLength(0);
            int roomHeight = tiles.GetLength(1);
            Bitmap roomBitmap = new(roomWidth * MetaTile.META_TILE_SIZE, roomHeight * MetaTile.META_TILE_SIZE);
            for (int x = 0; x < roomWidth; x++)
            {
                for (int y = 0; y < roomHeight; y++)
                {
                    MetaTile tile = tiles[x, y];
                    if (tile != null)
                    {
                        for (int i = 0; i < MetaTile.META_TILE_SIZE; i++)
                        {
                            for (int j = 0; j < MetaTile.META_TILE_SIZE; j++)
                            {
                                bool isCheckerBlack = (x + y) % 2 == 0; // Black for even sums, green for odd sums
                                Color color = isCheckerBlack ? Color.Black : Color.Black;

                                // Assuming the metaTile size matches the Cells array size for simplicity
                                // Check if the cell is a platform for color, otherwise use the checkerboard logic
                                color = tile.Cells[i, j] == CellType.Platform ? color : Color.White;
                                roomBitmap.SetPixel(x * MetaTile.META_TILE_SIZE + i, y * MetaTile.META_TILE_SIZE + j, color);
                            }
                        }
                    }
                    else
                    {
                        // Fill the missing metaTile area with white
                        for (int i = 0; i < MetaTile.META_TILE_SIZE; i++)
                        {
                            for (int j = 0; j < MetaTile.META_TILE_SIZE; j++)
                            {
                                roomBitmap.SetPixel(x * MetaTile.META_TILE_SIZE + i, y * MetaTile.META_TILE_SIZE + j, Color.White);
                            }
                        }
                    }
                }
            }
            return roomBitmap;
        }
    }
    public class MetaTile
    {
        public const int META_TILE_SIZE = 10;
        public CellType[,] Cells { get; private set; }
        public Connectivity ConnectivityRules { get; private set; }
        public MetaTile()
        {
            Cells = new CellType[META_TILE_SIZE, META_TILE_SIZE];
            for (int y = 0; y < META_TILE_SIZE; y++)
            {
                for (int x = 0; x < META_TILE_SIZE; x++)
                {
                    Cells[x, y] = CellType.Air;
                }
            }
            ConnectivityRules = Connectivity.All;
        }
        public MetaTile(Connectivity connectivity) : this()
        {
            ConnectivityRules = connectivity;
            InitializeWalls();
        }
        public CellType this[int x, int y]
        {
            get => Cells[x, y];
            set => Cells[x, y] = value;
        }
        private void InitializeWalls()
        {
            // Calculate start and end points for the opening in the middle of each wall
            int openingStart = META_TILE_SIZE / 5; // Start at 25% to create a 50% opening
            int openingEnd = META_TILE_SIZE - openingStart; // End at 75%

            for (int x = 0; x < META_TILE_SIZE; x++)
            {
                for (int y = 0; y < META_TILE_SIZE; y++)
                {
                    // Determine if we're on an edge
                    bool isOnEdge = x == 0 || x == META_TILE_SIZE - 1 || y == 0 || y == META_TILE_SIZE - 1;

                    // Check for connectivity and if the current position falls within the opening range
                    bool shouldHaveOpening = (
                        (x == 0 && ConnectivityRules.HasFlag(Connectivity.Left) && (y >= openingStart && y < openingEnd)) ||
                        (x == META_TILE_SIZE - 1 && ConnectivityRules.HasFlag(Connectivity.Right) && (y >= openingStart && y < openingEnd)) ||
                        (y == 0 && ConnectivityRules.HasFlag(Connectivity.Top) && (x >= openingStart && x < openingEnd)) ||
                        (y == META_TILE_SIZE - 1 && ConnectivityRules.HasFlag(Connectivity.Bottom) && (x >= openingStart && x < openingEnd))
                    );

                    // If we're on an edge but not within the opening range, it's a wall
                    if (isOnEdge && !shouldHaveOpening)
                    {
                        Cells[x, y] = CellType.Platform;
                    }
                    else
                    {
                        Cells[x, y] = CellType.Air; // Everything else is air, including the hole
                    }
                }
            }
        }
        public static MetaTile GenerateMetaTile(Connectivity connectivity)
        {
            return new MetaTile(connectivity);
        }
    }
    public enum CellType
    {
        Air,
        Platform
    }
    [Flags]
    public enum Connectivity
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        All = Left | Right | Top | Bottom
    }
}
