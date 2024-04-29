using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Text.Json;
namespace WinFormsApp4
{
    public partial class Form1 : Form
    {
        //public Room room;

        private static Form1 _instance = null;
        public static Form1 Instance => _instance ??= new Form1();
        TileType selectedType = TileType.Platform;
        private bool isMousePressed = false; // Track if the mouse is pressed
        private Dictionary<string, Connectivity> controlTextToConnectivity = new Dictionary<string, Connectivity>
        {
            ["TOP"] = Connectivity.Top,
            ["BOTTOM"] = Connectivity.Bottom,
            ["LEFT"] = Connectivity.Left,
            ["RIGHT"] = Connectivity.Right
        };
        Room previewRoom;
        MetaTile currentTile;
        private int currentMetatileIndex = -1; // Start with -1 indicating no selection
        public static List<MetaTile> metatileList = new();
        public Form1()
        {
            InitializeComponent();
            currentTile = new();
            KeyPreview = true;
            currentTile = new();
            DoubleBuffered = true;

            MouseWheel += Form_MouseWheel;
            TileCanvas.MouseDown += TileCanvas_MouseDown;
            TileCanvas.MouseMove += TileCanvas_MouseMove;
            TileCanvas.MouseUp += TileCanvas_MouseUp;
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadMetatiles();
            buttonBottom.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonBottom);
            buttonLeft.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonLeft);
            buttonRight.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonRight);
            buttonTop.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonTop);
            SetTitle();
        }
        private void TileCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isMousePressed = true; // Set flag to true when mouse button is pressed
            ModifyTile(sender, e); // Call the tile modification logic
        }

        private void TileCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMousePressed) // Check if the mouse button is still pressed
            {
                ModifyTile(sender, e); // Continue modifying tiles if the mouse is dragged
            }
        }

        private void TileCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            isMousePressed = false; // Reset flag when mouse button is released
        }

        private void Form_MouseWheel(object sender, MouseEventArgs e)
        {
            bool scrollUp = e.Delta > 0;

            // Convert the enum to a list to easily navigate through it
            var tileTypes = Enum.GetValues(typeof(TileType)).Cast<TileType>().ToList();
            int currentIndex = tileTypes.IndexOf(selectedType);

            // Cycle through the tile types based on scroll direction
            if (scrollUp)
            {
                // Scroll up: move to the next tile type, loop to the start if at the end
                currentIndex = (currentIndex + 1) % tileTypes.Count;
            }
            else
            {
                // Scroll down: move to the previous tile type, loop to the end if at the start
                currentIndex = (currentIndex - 1 + tileTypes.Count) % tileTypes.Count;
            }

            // Update the selected tile type
            selectedType = tileTypes[currentIndex];
            SetTitle();

        }
        private void SetTitle()
        {
            Text = $"Selected type: {selectedType} | Selected metatile: {(currentMetatileIndex == -1 ? "New metatile" : $"{currentMetatileIndex + 1}/{metatileList.Count}")}";
        }

        private void ToggleEntryPoint(Control control)
        {
            if (controlTextToConnectivity.TryGetValue(control.Text, out Connectivity connectivity))
            {
                // Toggle the specific connectivity bit using bitwise XOR
                currentTile.Connectivity ^= connectivity;
                UpdateSideColor(control);
            }
        }

        private void ModifyTile(object sender, MouseEventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            int controlWidth = pictureBox.Width;
            int controlHeight = pictureBox.Height;

            // Calculate the size of a tile based on the PictureBox size
            float tileSizeWidth = (float)controlWidth / MetaTile.META_TILE_SIZE;
            float tileSizeHeight = (float)controlHeight / MetaTile.META_TILE_SIZE;

            // Calculate the tile index based on the cursor position
            int tileX = (int)(e.X / tileSizeWidth);
            int tileY = (int)(e.Y / tileSizeHeight);

            // Ensure tileX and tileY are within bounds
            tileX = Math.Max(0, Math.Min(tileX, MetaTile.META_TILE_SIZE - 1));
            tileY = Math.Max(0, Math.Min(tileY, MetaTile.META_TILE_SIZE - 1));

            if (tileX < MetaTile.META_TILE_SIZE && tileY < MetaTile.META_TILE_SIZE)
            {
                if (e.Button == MouseButtons.Right)
                {
                    // Increment the TileType by 1, loop around if necessary
                    currentTile[tileX, tileY] = TileType.Air;
                }
                else if (e.Button == MouseButtons.Left)
                {
                    // Set the tile to the selected type on left-click
                    currentTile[tileX, tileY] = selectedType;
                }
                UpdateMetatileConnectivity();

                pictureBox.Invalidate(); // Trigger repaint
            }
        }
        private void UpdateSideColor(Control control)
        {
            // Use the control's text to find the corresponding Connectivity value
            if (controlTextToConnectivity.TryGetValue(control.Text, out Connectivity connectivity))
            {
                // Determine if the current tile's openings include this connectivity
                bool isOpen = (currentTile.Connectivity & connectivity) != 0;
                control.BackColor = isOpen ? Color.Green : Color.Red;
            }
        }
        public void CalculateTileUsageStatistics()
        {
            Dictionary<Connectivity, int> counts = [];
            for (int i = 0; i < 16; i++)
            {
                counts.Add((Connectivity)i, 0);
            }
            foreach (MetaTile tile in metatileList)
            {
                counts[tile.Connectivity]++;
            }
            listBoxStatistics.Items.Clear();
            foreach (var entry in counts)
            {
                string displayText = $"{entry.Key}: {entry.Value}";
                listBoxStatistics.Items.Add(displayText);
            }
        }
        private void UpdateMetatileConnectivity()
        {
            int middle = MetaTile.META_TILE_SIZE / 2;
            bool isSizeEven = MetaTile.META_TILE_SIZE % 2 == 0;
            currentTile.Connectivity = Connectivity.None; // Start with no connectivity

            // Adjust the logic to check for closed sides based on occupation
            // For even-sized metatiles, check two middle cells; for odd, just the center cell
            if (isSizeEven)
            {
                currentTile.Connectivity |=
                    (currentTile[middle - 1, 0] != TileType.Air || currentTile[middle, 0] != TileType.Air) ? 0 : Connectivity.Top;
                currentTile.Connectivity |=
                    (currentTile[middle - 1, MetaTile.META_TILE_SIZE - 1] != TileType.Air || currentTile[middle, MetaTile.META_TILE_SIZE - 1] != TileType.Air) ? 0 : Connectivity.Bottom;
                currentTile.Connectivity |=
                    (currentTile[0, middle - 1] != TileType.Air || currentTile[0, middle] != TileType.Air) ? 0 : Connectivity.Left;
                currentTile.Connectivity |=
                    (currentTile[MetaTile.META_TILE_SIZE - 1, middle - 1] != TileType.Air || currentTile[MetaTile.META_TILE_SIZE - 1, middle] != TileType.Air) ? 0 : Connectivity.Right;
            }
            else
            {
                currentTile.Connectivity |=
                    (currentTile[middle, 0] != TileType.Air) ? 0 : Connectivity.Top;
                currentTile.Connectivity |=
                    (currentTile[middle, MetaTile.META_TILE_SIZE - 1] != TileType.Air) ? 0 : Connectivity.Bottom;
                currentTile.Connectivity |=
                    (currentTile[0, middle] != TileType.Air) ? 0 : Connectivity.Left;
                currentTile.Connectivity |=
                    (currentTile[MetaTile.META_TILE_SIZE - 1, middle] != TileType.Air) ? 0 : Connectivity.Right;
            }

            // Update the UI to reflect the new connectivity state
            UpdateSideColor(buttonTop);
            UpdateSideColor(buttonBottom);
            UpdateSideColor(buttonLeft);
            UpdateSideColor(buttonRight);
        }


        private void TileCanvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            int cellW = ((Control)sender).Width / MetaTile.META_TILE_SIZE;
            int cellH = ((Control)sender).Height / MetaTile.META_TILE_SIZE;
            for (int y = 0; y < MetaTile.META_TILE_SIZE; y++)
            {
                for (int x = 0; x < MetaTile.META_TILE_SIZE; x++)
                {
                    switch (currentTile[x, y])
                    {
                        case TileType.Air:
                            break;
                        case TileType.Platform:
                            e.Graphics.FillRectangle(Brushes.Black, x * cellW, y * cellH, cellW, cellH);
                            break;
                        case TileType.Portal:
                            e.Graphics.FillRectangle(Brushes.Blue, x * cellW, y * cellH, cellW, cellH);
                            break;
                    }
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.N)
            {
                currentTile = currentTile.DeepCopy();
                currentMetatileIndex = -1;
                UpdateUI();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.C)
            {
                ResetCurrentMetatile();
                UpdateUI();
            }
            else if (e.KeyCode == Keys.S)
            {
                // Save the current metatile to the list
                SaveCurrentMetatile(); // Assuming this method adds the metatile to your list
                e.Handled = true; // Prevent further processing of the key event
                ShowToastNotification($"Saved. {metatileList.Count} metatiles are present.");
            }
            else if (e.KeyCode == Keys.A) // Select the previous metatile
            {
                if (metatileList.Count > 0)
                {
                    currentMetatileIndex = (currentMetatileIndex - 1 + metatileList.Count) % metatileList.Count;
                    LoadMetatileFromList(currentMetatileIndex);
                }
                SetTitle();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D) // Select the next metatile
            {
                if (metatileList.Count > 0)
                {
                    currentMetatileIndex = (currentMetatileIndex + 1) % metatileList.Count;
                    LoadMetatileFromList(currentMetatileIndex);
                }
                SetTitle();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.R)
            {
                if (currentMetatileIndex != -1)
                {
                    metatileList.RemoveAt(currentMetatileIndex);
                    ResetCurrentMetatile();
                    UpdateUI();
                }
            }
            else if (e.KeyCode == Keys.G)
            {
                RoomGenerator gen = new(4, 4);
                previewRoom = gen.GenerateRoom(metatileList); // Assuming GeneratePreviewRoom is your method
                RoomCanvas.BackgroundImageLayout = ImageLayout.Stretch;
                RoomCanvas.SizeMode = PictureBoxSizeMode.Normal;
                RoomCanvas.BackgroundImage = previewRoom.GenerateBitmap(); // Generate and set the background image
                e.Handled = true; // Mark the event as handled
            }
        }
        private void LoadMetatileFromList(int index)
        {
            if (index >= 0 && index < metatileList.Count)
            {
                // Assuming you have a method or logic to display or edit the selected metatile
                // For example, this could update the UI to reflect the metatile at 'index'
                currentTile = metatileList[index].DeepCopy(); // Set the currentTile to the selected one for editing
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            UpdateSideColor(buttonTop);
            UpdateSideColor(buttonLeft);
            UpdateSideColor(buttonRight);
            UpdateSideColor(buttonBottom);
            SetTitle();
            TileCanvas.Invalidate(); // Custom method to refresh the display/editor
        }

        public void ResetCurrentMetatile()
        {
            currentTile = new();
            currentMetatileIndex = -1;
        }
        public void SaveCurrentMetatile()
        {
            if (currentMetatileIndex != -1)
            {
                metatileList[currentMetatileIndex] = currentTile;
            }
            else
            {
                metatileList.Add(currentTile);
                currentTile = currentTile.DeepCopy();
            }
            CalculateTileUsageStatistics();
        }
        public void SaveMetatiles()
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            string jsonString = JsonSerializer.Serialize(metatileList, options);

            // Combine the base directory with your 'Data' folder and the filename
            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory); // Ensure the Data directory exists
            }
            string filePath = Path.Combine(dataDirectory, "metatiles.json");

            File.WriteAllText(filePath, jsonString);
            ShowToastNotification($"Saved {metatileList.Count} tiles to {filePath}.");
        }

        public void LoadMetatiles()
        {
            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            string filePath = Path.Combine(dataDirectory, "metatiles.json");
            string jsonString = "";
            if (!File.Exists(filePath))
            {
                ShowToastNotification("No saved tiles found.");
            }
            else
            {
                jsonString = File.ReadAllText(filePath);
            }
            if (!string.IsNullOrEmpty(jsonString))
            {
                metatileList = JsonSerializer.Deserialize<List<MetaTile>>(jsonString);
                ShowToastNotification($"Loaded {metatileList!.Count} metatiles.");
            }
            else
            {
                ShowToastNotification("No saved tiles found.");
            }
            CalculateTileUsageStatistics();
        }
        private ToastForm toastForm;

        private void ShowToastNotification(string message, int duration = 1000)
        {
            // Close existing toast if any
            toastForm?.Close();

            toastForm = new ToastForm(message, duration);
            toastForm.Show();
            toastForm.PositionRelativeToForm(this);
        }

        // Main form's Move event handler
        private void Form1_Move(object sender, EventArgs e)
        {
            if (toastForm != null && !toastForm.IsDisposed)
            {
                toastForm.PositionRelativeToForm(this);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveMetatiles();
        }
        ToolTip toolTip = new();
        private void RoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (previewRoom == null)
            {
                return;
            }
            // Assuming 'roomWidth' and 'roomHeight' represent the dimensions of the room in metatiles
            int controlWidth = RoomCanvas.Width;
            int controlHeight = RoomCanvas.Height;

            // Calculate the size of a metatile based on the PictureBox size and room dimensions
            float metatilePixelWidth = (float)controlWidth / previewRoom.Width;
            float metatilePixelHeight = (float)controlHeight / previewRoom.Height;

            // Calculate the metatile index based on the cursor position
            int metatileX = (int)(e.X / metatilePixelWidth);
            int metatileY = (int)(e.Y / metatilePixelHeight);

            // Ensure metatileX and metatileY are within bounds
            metatileX = Math.Max(0, Math.Min(metatileX, previewRoom.Width - 1));
            metatileY = Math.Max(0, Math.Min(metatileY, previewRoom.Height - 1));

            // Assuming a method to get a metatile's connectivity based on its position in the room
            var metatile = previewRoom[metatileX, metatileY];
            if (metatile != null)
            {
                // Here, update your toolTip or UI element to show the metatile's info
                string toolTipText = $"Metatile: [{metatileX}, {metatileY}] | Connectivity: {metatile.Connectivity}";
                toolTip.SetToolTip(RoomCanvas, toolTipText);
            }
            else
            {
                toolTip.SetToolTip(RoomCanvas, "Empty space");
            }
        }

        private void RoomCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (RoomCanvas.BackgroundImage != null)
            {
                // Set the interpolation mode
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                // Calculate the scale factor to maintain aspect ratio
                float scaleX = (float)RoomCanvas.Width / RoomCanvas.BackgroundImage.Width;
                float scaleY = (float)RoomCanvas.Height / RoomCanvas.BackgroundImage.Height;
                float scale = Math.Min(scaleX, scaleY);

                // Calculate the position to center the image
                int posX = (int)((RoomCanvas.Width - (RoomCanvas.BackgroundImage.Width * scale)) / 2);
                int posY = (int)((RoomCanvas.Height - (RoomCanvas.BackgroundImage.Height * scale)) / 2);

                // Draw the image
                e.Graphics.DrawImage(RoomCanvas.BackgroundImage, new Rectangle(posX, posY, (int)(RoomCanvas.BackgroundImage.Width * scale), (int)(RoomCanvas.BackgroundImage.Height * scale)));
            }
        }
    }
    public enum TileType
    {
        Air,
        Platform,
        Portal
    }
    public static class ConnectivityExtensions
    {
        public static IEnumerable<Connectivity> GetIndividualFlags(Connectivity connectivity)
        {
            foreach (Connectivity flag in Enum.GetValues(typeof(Connectivity)))
            {
                if (flag == Connectivity.None)
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
        private Queue<(int x, int y, Connectivity directionFrom)> expansionQueue = new();
        private Random rng = new Random();

        public RoomGenerator(int roomWidth, int roomHeight)
        {
            this.roomWidth = roomWidth;
            this.roomHeight = roomHeight;
            this.roomLayout = new MetaTile[roomWidth, roomHeight];
        }

        public Room GenerateRoom(List<MetaTile> metaTiles = null)
        {
            bool usePredefinedMetatiles = metaTiles != null && metaTiles.Any();
            int metatilePlaced;
            int tries = 0;
            do
            {
                metatilePlaced = 0;
                tries++;
                expansionQueue.Clear();
                roomLayout = new MetaTile[roomWidth, roomHeight];
                Debug.WriteLine($"try #{tries}");
                // Initialize with a starting MetaTile that opens in at least one direction
                int startX = rng.Next(roomWidth);
                int startY = 0;
                Debug.WriteLine("starting at " + startX + ", " + startY);
                if (usePredefinedMetatiles)
                {
                    // Select a random metatile from the provided list
                    roomLayout[startX, startY] = metaTiles[rng.Next(metaTiles.Count)].DeepCopy(); // Use DeepCopy if necessary to avoid modifying the original
                }
                else
                {
                    Connectivity startDirection = (Connectivity)rng.Next(1, 16); // Exclude None, include All
                    roomLayout[startX, startY] = MetaTile.GenerateMetaTile(startDirection);
                }

                // Add open directions to the queue for the starting tile
                AddToQueue(startX, startY, roomLayout[startX, startY].Connectivity);

                while (expansionQueue.Count > 0)
                {
                    var (x, y, directionFrom) = expansionQueue.Dequeue();
                    Debug.WriteLine("continuing at " + x + ", " + y);

                    TryExpand(x, y, directionFrom, metaTiles);
                    metatilePlaced++;
                }
            } while (metatilePlaced < Math.Min(roomWidth, roomHeight) && tries < 5);

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
        private void TryExpand(int x, int y, Connectivity directionFrom, List<MetaTile> metaTiles = null)
        {
            bool usePredefinedMetatiles = metaTiles != null && metaTiles.Any();

            if (!IsInBounds(x, y) || roomLayout[x, y] != null) return;

            if (usePredefinedMetatiles)
            {
                // Select a random metatile from the list, ensuring it can connect in the required direction
                MetaTile selectedTile = null;
                var compatibleTiles = metaTiles.Where(mt => ConnectivityExtensions.GetIndividualFlags(mt.Connectivity).Contains(directionFrom)).ToList();
                if (compatibleTiles.Count != 0)
                {
                    selectedTile = compatibleTiles[rng.Next(compatibleTiles.Count)].DeepCopy(); // Use DeepCopy to avoid modifying the original
                }
                roomLayout[x, y] = selectedTile ?? MetaTile.GenerateMetaTile(Connectivity.None); // Fallback to a closed tile if no compatible tile found
            }
            else
            {
                // Original logic for generating a new metatile based on connectivity
                Connectivity newTileConnectivity = directionFrom | GetRandomConnectivityExcluding(directionFrom);
                roomLayout[x, y] = MetaTile.GenerateMetaTile(newTileConnectivity);
            }

            // Proceed with adding to the queue using the newly placed tile's connectivity
            if (roomLayout[x, y] != null)
            {
                AddToQueue(x, y, roomLayout[x, y].Connectivity);
            }
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
        public int Width => tiles.GetLength(0);
        public int Height => tiles.GetLength(1);
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
                                color = tile[i, j] == TileType.Platform ? color : Color.White;
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
        public Connectivity Connectivity { get; set; }
        public TileType[] Tiles { get; set; }
        public MetaTile(Connectivity connectivity) : this()
        {
            Connectivity = connectivity;
            InitializeWalls();
        }
        public MetaTile()
        {
            Tiles = new TileType[META_TILE_SIZE * META_TILE_SIZE]; // Initialize as a 1D array
            Connectivity = Connectivity.None;
            ResetTiles();
        }

        // Update the indexer to work with a 1D array
        public TileType this[int x, int y]
        {
            get => Tiles[y * META_TILE_SIZE + x];
            set => Tiles[y * META_TILE_SIZE + x] = value;
        }

        // Adjust DeepCopy method for 1D array
        public MetaTile DeepCopy()
        {
            MetaTile copy = new();
            Array.Copy(this.Tiles, copy.Tiles, this.Tiles.Length); // Use Array.Copy for efficiency

            copy.Connectivity = this.Connectivity;

            return copy;
        }

        // Adjust ResetTiles method for 1D array
        public void ResetTiles()
        {
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = TileType.Air; // Reset each tile to Air
            }

            // Reset all openings to true
            Connectivity = Connectivity.All;
        }
        private void InitializeWalls()
        {
            // Calculate start and end points for the opening in the middle of each wall
            int openingStart = META_TILE_SIZE / 5; // Start at 25% to create a 50% opening
            int openingEnd = META_TILE_SIZE - openingStart; // End at 75%
            if (Connectivity == Connectivity.None)
            {
                for (int x = 0; x < META_TILE_SIZE; x++)
                {
                    for (int y = 0; y < META_TILE_SIZE; y++)
                    {
                        this[x, y] = TileType.Platform;
                    }
                }
            }
            else
            {
                for (int x = 0; x < META_TILE_SIZE; x++)
                {
                    for (int y = 0; y < META_TILE_SIZE; y++)
                    {
                        // Determine if we're on an edge
                        bool isOnEdge = x == 0 || x == META_TILE_SIZE - 1 || y == 0 || y == META_TILE_SIZE - 1;

                        // Check for connectivity and if the current position falls within the opening range
                        bool shouldHaveOpening = (
                            (x == 0 && Connectivity.HasFlag(Connectivity.Left) && (y >= openingStart && y < openingEnd)) ||
                            (x == META_TILE_SIZE - 1 && Connectivity.HasFlag(Connectivity.Right) && (y >= openingStart && y < openingEnd)) ||
                            (y == 0 && Connectivity.HasFlag(Connectivity.Top) && (x >= openingStart && x < openingEnd)) ||
                            (y == META_TILE_SIZE - 1 && Connectivity.HasFlag(Connectivity.Bottom) && (x >= openingStart && x < openingEnd))
                        );

                        // If we're on an edge but not within the opening range, it's a wall
                        if (isOnEdge && !shouldHaveOpening)
                        {
                            this[x, y] = TileType.Platform;
                        }
                        else
                        {
                            this[x, y] = TileType.Air; // Everything else is air, including the hole
                        }
                    }
                }
            }
        }
        public static MetaTile GenerateMetaTile(Connectivity connectivity)
        {
            return new MetaTile(connectivity);
        }
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
