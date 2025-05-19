using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace WinFormsApp4
{
    public partial class Form1 : Form
    {
        private static Form1? _instance = null;
        public static Form1 Instance => _instance ??= new Form1();
        TileType selectedType = TileType.Platform;
        private bool isMousePressed = false;
        private static readonly Dictionary<string, Connectivity> controlTextToConnectivity = new()
        {
            ["TOP"] = Connectivity.Top,
            ["BOTTOM"] = Connectivity.Bottom,
            ["LEFT"] = Connectivity.Left,
            ["RIGHT"] = Connectivity.Right
        };
        Room? previewRoom;
        MetaTile currentTile;
        private int currentMetatileIndex = -1; // Start with -1 indicating no selection
        public static List<MetaTile> metatileList = [];
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
            buttonBottom.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonBottom);
            buttonLeft.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonLeft);
            buttonRight.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonRight);
            buttonTop.Click += (sender, e) => ToggleEntryPoint((Control)sender!);
            UpdateSideColor(buttonTop);
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadMetatiles();

            SetTitle();
        }
        private void TileCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isMousePressed = true; // Set flag to true when mouse button is pressed
            ModifyTile(sender, e); // Call the tile modification logic
        }
        private Point? lastMousePosition = null;
        private void TileCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMousePressed)
            {
                if (lastMousePosition.HasValue)
                {
                    // Draw a smooth line of tiles between the last and current position
                    DrawInterpolatedTiles(lastMousePosition.Value, e);
                }
                else
                {
                    ModifyTile(sender, e);
                }

                lastMousePosition = new Point(e.X, e.Y);
            }
        }

        private void TileCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            isMousePressed = false;
            lastMousePosition = null; // Reset when drag ends
        }

        private void DrawInterpolatedTiles(Point start, MouseEventArgs e)
        {
            int dx = e.Location.X - start.X;
            int dy = e.Location.Y - start.Y;

            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (steps == 0) // Prevent division by zero
            {
                ModifyTile(TileCanvas, new MouseEventArgs(e.Button, 1, start.X, start.Y, 0));
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                int x = start.X + dx * i / steps;
                int y = start.Y + dy * i / steps;

                ModifyTile(TileCanvas, new MouseEventArgs(e.Button, 1, x, y, 0));
            }
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
            PictureBox pictureBox = (sender as PictureBox)!;
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
            switch (e.KeyCode)
            {
                case Keys.N:
                    currentTile = currentTile.DeepCopy();
                    currentMetatileIndex = -1;
                    UpdateUI();
                    e.Handled = true;
                    break;
                case Keys.C:
                    ResetCurrentMetatile();
                    UpdateUI();
                    break;
                case Keys.S:
                    SaveCurrentMetatile();
                    e.Handled = true;
                    ShowToastNotification($"Saved. {metatileList.Count} metatiles are present.");
                    break;
                case Keys.A:
                    if (metatileList.Count > 0)
                    {
                        currentMetatileIndex = (currentMetatileIndex - 1 + metatileList.Count) % metatileList.Count;
                        LoadMetatileFromList(currentMetatileIndex);
                    }
                    SetTitle();
                    e.Handled = true;
                    break;
                case Keys.D:
                    if (metatileList.Count > 0)
                    {
                        currentMetatileIndex = (currentMetatileIndex + 1) % metatileList.Count;
                        LoadMetatileFromList(currentMetatileIndex);
                    }
                    SetTitle();
                    e.Handled = true;
                    break;
                case Keys.R:
                    if (currentMetatileIndex != -1)
                    {
                        metatileList.RemoveAt(currentMetatileIndex);
                        ResetCurrentMetatile();
                        UpdateUI();
                    }
                    break;
                case Keys.G:
                    {
                        RoomGenerator gen = new(6, 6);
                        previewRoom = gen.GenerateRoom(metatileList);
                        RoomCanvas.BackgroundImageLayout = ImageLayout.Stretch;
                        RoomCanvas.SizeMode = PictureBoxSizeMode.Normal;
                        RoomCanvas.BackgroundImage = previewRoom.GenerateBitmap(); // Generate and set the background image
                        e.Handled = true; // Mark the event as handled
                        break;
                    }
            }
        }
        private void LoadMetatileFromList(int index)
        {
            if (index >= 0 && index < metatileList.Count)
            {
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
        private static JsonSerializerOptions options = new() { WriteIndented = false };
        public void SaveMetatiles()
        {
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
                metatileList = JsonSerializer.Deserialize<List<MetaTile>>(jsonString) ?? [];
                ShowToastNotification($"Loaded {metatileList.Count} metatiles.");
            }
            else
            {
                ShowToastNotification("No saved tiles found.");
            }
            CalculateTileUsageStatistics();
        }
        private ToastForm? toastForm;

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
            if (RoomCanvas.BackgroundImage == null) return;

            // Scale and center the background image (same as before)
            float scaleX = (float)RoomCanvas.Width / RoomCanvas.BackgroundImage.Width;
            float scaleY = (float)RoomCanvas.Height / RoomCanvas.BackgroundImage.Height;
            float scale = Math.Min(scaleX, scaleY);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int posX = (int)((RoomCanvas.Width - (RoomCanvas.BackgroundImage.Width * scale)) / 2);
            int posY = (int)((RoomCanvas.Height - (RoomCanvas.BackgroundImage.Height * scale)) / 2);

            // Draw the background image centered+scaled
            e.Graphics.DrawImage(
                RoomCanvas.BackgroundImage,
                new Rectangle(posX, posY, (int)(RoomCanvas.BackgroundImage.Width * scale), (int)(RoomCanvas.BackgroundImage.Height * scale))
            );
            if (previewRoom == null) return;
            if (DrawGridCheckBox.Checked)
            {
                for (int y = 0; y < previewRoom.Height; y++)
                {
                    e.Graphics.DrawLine(
                        Pens.Green,
                        0,
                        y * (RoomCanvas.Width / previewRoom.Width),
                        RoomCanvas.Width,
                        y * (RoomCanvas.Width / previewRoom.Width));
                }
                for (int x = 0; x < previewRoom.Width; x++)
                {
                    e.Graphics.DrawLine(
                        Pens.Green,
                        x * (RoomCanvas.Width / previewRoom.Width),
                        0,
                        x * (RoomCanvas.Width / previewRoom.Width),
                        RoomCanvas.Height);
                }
            }

            if (DrawHistoryCheckBox.Checked)
            {
                using Pen arrowPen = new(Color.Red, 2);
                arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3, 5);

                // For each expansion record (fx, fy) -> (tx, ty)
                float tileWidth = (float)RoomCanvas.Width / previewRoom.Width;
                float tileHeight = (float)RoomCanvas.Height / previewRoom.Height;

                foreach (var (fx, fy, tx, ty) in RoomGenerator.expansionRecords)
                {
                    // Center of each tile in "grid space"
                    float fromX = (fx + 0.5f) * tileWidth;
                    float fromY = (fy + 0.5f) * tileHeight;
                    float toX = (tx + 0.5f) * tileWidth;
                    float toY = (ty + 0.5f) * tileHeight;

                    // Draw the arrow from parent tile to child tile
                    e.Graphics.DrawLine(arrowPen, fromX, fromY, toX, toY);
                }
            }
        }


        private void Form1_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            const string tutorialMessage =
                "Keyboard Shortcuts:\n\n" +
                "N - Create a new tile.\n" +
                "C - Reset the current metatile.\n" +
                "S - Save the current metatile to the list.\n" +
                "A - Select the previous metatile.\n" +
                "D - Select the next metatile.\n" +
                "R - Remove the selected metatile.\n" +
                "G - Generate a preview room using metatiles.\n\n" +
                "Use these shortcuts to quickly manipulate and navigate your metatile list.";

            MessageBox.Show(tutorialMessage, "Application Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RoomCanvas_Click(object sender, EventArgs e)
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
            int metatileX = (int)(RoomCanvas.PointToClient(Cursor.Position).X / metatilePixelWidth);
            int metatileY = (int)(RoomCanvas.PointToClient(Cursor.Position).Y / metatilePixelHeight);

            // Ensure metatileX and metatileY are within bounds
            metatileX = Math.Max(0, Math.Min(metatileX, previewRoom.Width - 1));
            metatileY = Math.Max(0, Math.Min(metatileY, previewRoom.Height - 1));

            // Assuming a method to get a metatile's connectivity based on its position in the room
            var metatile = previewRoom[metatileX, metatileY];
            Debug.WriteLine($"Metatile at {metatileX};{metatileY}");
            if (metatile != null)
            {
                DumpMetaTile(metatile);
            }
        }
        /// <summary>
        /// Dumps a visual representation of a MetaTile to the console.
        /// W = wall, . = air, O = an opening based on connectivity.
        /// </summary>
        private static void DumpMetaTile(MetaTile tile)
        {
            int size = MetaTile.META_TILE_SIZE;
            StringBuilder sb = new();

            sb.AppendLine($"MetaTile Dump (Connectivity: {tile.Connectivity}):");

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (tile[x, y] == TileType.Platform)
                    {
                        sb.Append('W');  // Wall
                    }
                    else
                    {
                        sb.Append('.');  // Air
                    }
                }
                sb.AppendLine();
            }

            // Connectivity markers
            sb.AppendLine("Connectivity:");
            sb.AppendLine($"  Top:    {(tile.Connectivity.HasFlag(Connectivity.Top) ? "O" : "W")}");
            sb.AppendLine($"  Bottom: {(tile.Connectivity.HasFlag(Connectivity.Bottom) ? "O" : "W")}");
            sb.AppendLine($"  Left:   {(tile.Connectivity.HasFlag(Connectivity.Left) ? "O" : "W")}");
            sb.AppendLine($"  Right:  {(tile.Connectivity.HasFlag(Connectivity.Right) ? "O" : "W")}");

            Debug.WriteLine(sb.ToString());
        }

        private void Settings_Changed(object sender, EventArgs e)
        {
            RoomCanvas.Invalidate();
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
            for (int i = 1; i <= (int)Connectivity.All; i <<= 1)
            {
                Connectivity flag = (Connectivity)i;
                if (connectivity.HasFlag(flag))
                {
                    yield return flag;
                }
            }
        }
    }

    public class RoomGenerator
    {
        private struct ExpansionRequest
        {
            public int FromX;
            public int FromY;
            public int ToX;
            public int ToY;
            public Connectivity DirectionFrom;
        }
        private int roomWidth, roomHeight;
        private MetaTile[,] roomLayout;
        private Queue<ExpansionRequest> expansionQueue = new();
        private Random rng = new();

        public RoomGenerator(int roomWidth, int roomHeight)
        {
            this.roomWidth = roomWidth;
            this.roomHeight = roomHeight;
            this.roomLayout = new MetaTile[roomWidth, roomHeight];
        }

        public Room GenerateRoom(List<MetaTile>? metaTiles = null)
        {
            // If we have a predefined list of MetaTiles, expand it to include
            // all rotated/flipped variants.
            bool usePredefinedMetatiles = metaTiles != null && metaTiles.Count > 0;
            if (usePredefinedMetatiles)
            {
                var expanded = new List<MetaTile>();

                foreach (var mt in metaTiles!)
                {
                    // Original
                    expanded.Add(mt.DeepCopy());

                    // Rotations
                    var rot90 = RotateMetaTile(mt, 90);
                    var rot180 = RotateMetaTile(mt, 180);
                    var rot270 = RotateMetaTile(mt, 270);
                    expanded.Add(rot90);
                    expanded.Add(rot180);
                    expanded.Add(rot270);

                    // Horizontal flip of original
                    var flipped = FlipMetaTileHorizontal(mt);
                    expanded.Add(flipped);

                    // Rotate the flipped version
                    expanded.Add(RotateMetaTile(flipped, 90));
                    expanded.Add(RotateMetaTile(flipped, 180));
                    expanded.Add(RotateMetaTile(flipped, 270));
                }

                // Replace metaTiles with the expanded list
                metaTiles = expanded;
            }
            Debug.WriteLine($"We have {metaTiles?.Count} metatiles.");
            int metatilePlaced;
            int tries = 0;

            do
            {
                expansionRecords.Clear();
                metatilePlaced = 0;
                tries++;
                expansionQueue.Clear();
                roomLayout = new MetaTile[roomWidth, roomHeight];
                //Debug.WriteLine($"try #{tries}");

                // Pick a random start position at the top
                int startX = rng.Next(roomWidth);
                int startY = 0;
                //Debug.WriteLine("starting at " + startX + ", " + startY);

                if (usePredefinedMetatiles)
                {
                    // Randomly pick from the expanded set (DeepCopy to avoid referencing the same object).
                    roomLayout[startX, startY] = metaTiles![rng.Next(metaTiles.Count)].DeepCopy();
                }
                else
                {
                    // Random connectivity from (1..15) excludes None=0, includes all combos
                    Connectivity startDirection = (Connectivity)rng.Next(1, 16);
                    roomLayout[startX, startY] = MetaTile.GenerateMetaTile(startDirection);
                }

                // Enqueue any open directions from the starting tile
                AddToQueue(startX, startY, roomLayout[startX, startY].Connectivity);

                // Keep expanding while there's something in the queue
                while (expansionQueue.Count > 0)
                {
                    ExpansionRequest request = expansionQueue.Dequeue();
                    //Debug.WriteLine("continuing at " + request.ToX + ", " + request.ToY);

                    // Try to place new tiles branching from the current tile's open sides
                    TryExpand(request.FromX, request.FromY, request.ToX, request.ToY, request.DirectionFrom, metaTiles!);
                    metatilePlaced++;
                }
            }
            while (metatilePlaced < Math.Min(roomWidth, roomHeight) * 4 /*&& tries < 50*/);
            // Once we can’t expand further, fill the rest of the room with closed tiles
            FillRemainingWithClosedTiles();
            return new Room(roomLayout);
        }
        /// <summary>
        /// Rotates the given MetaTile by 0, 90, 180, or 270 degrees (clockwise).
        /// Returns a new MetaTile instance with the tile data and connectivity updated.
        /// </summary>
        private static MetaTile RotateMetaTile(MetaTile original, int degrees)
        {
            var copy = new MetaTile();
            int size = MetaTile.META_TILE_SIZE;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    (int nx, int ny) = degrees switch
                    {
                        90 => (size - 1 - y, x),
                        180 => (size - 1 - x, size - 1 - y),
                        270 => (y, size - 1 - x),
                        _ => (x, y)
                    };

                    copy[nx, ny] = original[x, y];
                }
            }

            copy.Connectivity = RotateConnectivity(original.Connectivity, degrees);
            return copy;
        }

        /// <summary>
        /// Adjusts the bitwise Connectivity to match a clockwise rotation by 0, 90, 180, or 270 degrees.
        /// </summary>
        private static Connectivity RotateConnectivity(Connectivity c, int degrees)
        {
            Connectivity result = Connectivity.None;

            switch (degrees)
            {
                case 90:
                    // Left -> Top, Top -> Right, Right -> Bottom, Bottom -> Left
                    if (c.HasFlag(Connectivity.Left)) result |= Connectivity.Top;
                    if (c.HasFlag(Connectivity.Top)) result |= Connectivity.Right;
                    if (c.HasFlag(Connectivity.Right)) result |= Connectivity.Bottom;
                    if (c.HasFlag(Connectivity.Bottom)) result |= Connectivity.Left;
                    break;
                case 180:
                    // Left -> Right, Right -> Left, Top -> Bottom, Bottom -> Top
                    if (c.HasFlag(Connectivity.Left)) result |= Connectivity.Right;
                    if (c.HasFlag(Connectivity.Right)) result |= Connectivity.Left;
                    if (c.HasFlag(Connectivity.Top)) result |= Connectivity.Bottom;
                    if (c.HasFlag(Connectivity.Bottom)) result |= Connectivity.Top;
                    break;
                case 270:
                    // Left -> Bottom, Bottom -> Right, Right -> Top, Top -> Left
                    if (c.HasFlag(Connectivity.Left)) result |= Connectivity.Bottom;
                    if (c.HasFlag(Connectivity.Bottom)) result |= Connectivity.Right;
                    if (c.HasFlag(Connectivity.Right)) result |= Connectivity.Top;
                    if (c.HasFlag(Connectivity.Top)) result |= Connectivity.Left;
                    break;
                // 0 degrees or default: no change
                default:
                    result = c;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Flips the tile horizontally (left edge becomes right edge) 
        /// and updates Connectivity accordingly.
        /// </summary>
        private static MetaTile FlipMetaTileHorizontal(MetaTile original)
        {
            var copy = new MetaTile();
            int size = MetaTile.META_TILE_SIZE;

            // Flip the tile data horizontally
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // (x, y) -> (size - 1 - x, y)
                    copy[size - 1 - x, y] = original[x, y];
                }
            }

            // Flip the connectivity bits
            copy.Connectivity = FlipConnectivityHorizontal(original.Connectivity);

            return copy;
        }

        /// <summary>
        /// Flips the Connectivity bits horizontally (Left <-> Right). 
        /// Top and Bottom remain the same.
        /// </summary>
        private static Connectivity FlipConnectivityHorizontal(Connectivity c)
        {
            Connectivity result = Connectivity.None;

            // Swap left and right
            if (c.HasFlag(Connectivity.Left)) result |= Connectivity.Right;
            if (c.HasFlag(Connectivity.Right)) result |= Connectivity.Left;

            // Keep top/bottom as they are
            if (c.HasFlag(Connectivity.Top)) result |= Connectivity.Top;
            if (c.HasFlag(Connectivity.Bottom)) result |= Connectivity.Bottom;

            return result;
        }
        private void AddToQueue(int x, int y, Connectivity direction)
        {
            // This method now adds all possible expansion directions to the queue based on the current tile's connectivity
            foreach (var directionFlag in ConnectivityExtensions.GetIndividualFlags(direction))
            {
                var (nextX, nextY) = GetNextCoordinates(x, y, directionFlag);
                if (IsInBounds(nextX, nextY) && roomLayout[nextX, nextY] == null)
                {
                    expansionQueue.Enqueue(new ExpansionRequest
                    {
                        FromX = x,
                        FromY = y,
                        ToX = nextX,
                        ToY = nextY,
                        DirectionFrom = GetOppositeDirection(directionFlag)
                    });
                }
            }
        }
        private static Connectivity GetOppositeDirection(Connectivity direction)
        {
            return direction switch
            {
                Connectivity.Left => Connectivity.Right,
                Connectivity.Right => Connectivity.Left,
                Connectivity.Top => Connectivity.Bottom,
                Connectivity.Bottom => Connectivity.Top,
                _ => Connectivity.None,
            };
        }
        public static List<(int fromX, int fromY, int toX, int toY)> expansionRecords = [];
        private void TryExpand(int fromX, int fromY, int x, int y, Connectivity directionFrom, List<MetaTile> metaTiles)
        {
            if (!IsInBounds(x, y) || roomLayout[x, y] != null) return;

            bool usePredefinedMetatiles = metaTiles != null && metaTiles.Count > 0;

            // (Choose the tile from metaTiles or generate a random tile)
            if (usePredefinedMetatiles)
            {
                // e.g. pick a tile that has 'directionFrom' in its connectivity
                var compatibleTiles = metaTiles!
                    .Where(mt => mt.Connectivity.HasFlag(directionFrom))
                    .ToList();

                if (compatibleTiles.Count > 0)
                {
                    roomLayout[x, y] = compatibleTiles[rng.Next(compatibleTiles.Count)].DeepCopy();
                }
                else
                {
                    // fallback
                    roomLayout[x, y] = MetaTile.GenerateMetaTile(Connectivity.None);
                }
            }
            else
            {
                // fallback to random connectivity
                Connectivity newTileConnectivity = directionFrom | GetRandomConnectivityExcluding(directionFrom);
                roomLayout[x, y] = MetaTile.GenerateMetaTile(newTileConnectivity);
            }

            // If tile is placed, record an expansion for arrow-drawing:
            if (roomLayout[x, y] != null)
            {
                // E.g., record expansions so we can draw arrows
                expansionRecords.Add((fromX, fromY, x, y));

                // Now queue new expansions from the newly placed tile
                AddToQueue(x, y, roomLayout[x, y].Connectivity);
            }
        }



        private static (int, int) GetNextCoordinates(int x, int y, Connectivity direction)
        {
            return direction switch
            {
                Connectivity.Left => (x - 1, y),
                Connectivity.Right => (x + 1, y),
                Connectivity.Top => (x, y - 1),
                Connectivity.Bottom => (x, y + 1),
                _ => (x, y),// Should never happen
            };
        }

        private bool IsInBounds(int x, int y) => x >= 0 && x < roomWidth && y >= 0 && y < roomHeight;

        private Connectivity GetRandomConnectivityExcluding(Connectivity exclude)
        {
            // Generate a random connectivity that does not include 'exclude'. This ensures variety in connectivity.
            Connectivity result;
            do
            {
                result = (Connectivity)rng.Next(1, 16);
            } while ((result & exclude) != 0);
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
                                Color color = tile[i, j] == TileType.Platform ? Color.Black : Color.White;
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
