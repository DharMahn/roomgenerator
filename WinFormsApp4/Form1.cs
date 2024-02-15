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
            RoomGenerator gen = new(10, 10);
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

    public class RoomGenerator(int roomWidth, int roomHeight)
    {
        private int roomWidth = roomWidth, roomHeight = roomHeight;
        private MetaTile[,] roomLayout = new MetaTile[roomWidth, roomHeight];
        private Queue<(int x, int y, Connectivity direction)> expansionQueue = new Queue<(int x, int y, Connectivity direction)>();
        private Random rng = new();

        public Room GenerateRoom()
        {
            // Initialize with a starting MetaTile that opens in at least one direction
            int startX = rng.Next(1, roomWidth - 1);
            int startY = rng.Next(1, roomHeight - 1);
            var startDirection = (Connectivity)rng.Next(15) + 1;
            roomLayout[startX, startY] = MetaTile.GenerateMetaTile(startDirection);
            Trace.WriteLine("Started at: " + startX + ";" + startY);
            // Add open directions to the queue
            foreach (Connectivity direction in ConnectivityExtensions.GetIndividualFlags(startDirection))
            {
                AddToQueue(startX, startY, direction);
            }

            // Process queue until empty
            while (expansionQueue.Count > 0)
            {
                var (x, y, direction) = expansionQueue.Dequeue();
                //Form1.Instance.BackgroundImage = new Room(roomLayout).GenerateBitmap();
                //Application.DoEvents();
                //Thread.Sleep(1000);
                TryExpand(x, y, direction);
            }

            // Fill the rest with closed tiles
            FillRemainingWithClosedTiles();

            return new Room(roomLayout);
        }

        private void AddToQueue(int x, int y, Connectivity direction)
        {
            int nextX = x, nextY = y;
            switch (direction)
            {
                case Connectivity.Left:
                    nextX--;
                    break;
                case Connectivity.Right:
                    nextX++;
                    break;
                case Connectivity.Top:
                    nextY--;
                    break;
                case Connectivity.Bottom:
                    nextY++;
                    break;
            }

            if (nextX >= 0 && nextX < roomWidth && nextY >= 0 && nextY < roomHeight && roomLayout[nextX, nextY] == null)
            {
                // Assuming we're always expanding from an open tile, we add the opposite direction to ensure connectivity
                Connectivity oppositeDirection = GetOppositeDirection(direction);
                expansionQueue.Enqueue((nextX, nextY, oppositeDirection));
            }
        }

        private void TryExpand(int x, int y, Connectivity directionFrom)
        {
            if (x < 0 || x >= roomWidth || y < 0 || y >= roomHeight || roomLayout[x, y] != null)
            {
                // Out of bounds or already filled
                return;
            }

            // Ensure the new tile has an opening back to the tile we're expanding from.
            Connectivity openingBack = GetOppositeDirection(directionFrom);

            // Generate the new tile with the opening back plus potentially more openings.
            Connectivity newTileConnectivity = openingBack | GetRandomConnectivity();
            Trace.WriteLine($"Expanding at: {x};{y} this is going to be a {newTileConnectivity} metatile");
            roomLayout[x, y] = MetaTile.GenerateMetaTile(newTileConnectivity);

            // Correctly enqueue possible expansion directions from this new tile.
            foreach (var possibleDirection in ConnectivityExtensions.GetIndividualFlags(newTileConnectivity))
            {
                if (possibleDirection != openingBack) // Ensure we're not adding the direction we came from
                {
                    AddToQueue(x, y, possibleDirection);
                }
            }
        }

        private Connectivity GetOppositeDirection(Connectivity direction)
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



        private Connectivity GetRandomConnectivity()
        {
            // Generate a random connectivity; this is a placeholder for more sophisticated logic.
            return (Connectivity)rng.Next(15) + 1;
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
                                Color color = isCheckerBlack ? Color.Green : Color.Black;

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
