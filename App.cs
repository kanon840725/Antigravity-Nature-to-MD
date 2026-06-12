using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace NatureToMD
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }

    public class MainWindow : Window
    {
        // UI Controls
        private Grid rootGrid;
        private Grid mainContentGrid;
        private Border toolbarBorder;
        private StackPanel toolbar;
        private RichTextBox visualEditor;
        private WebBrowser previewBrowser;
        private GridSplitter splitter;
        
        // Toolbar Dropdowns for Settings
        private TextBlock themeLabel;
        private ComboBox themeCombo;
        private TextBlock textColorLabel;
        private ComboBox colorCombo;

        // Columns
        private ColumnDefinition previewColumn;
        private ColumnDefinition splitterColumn;
        private Border statusBarBorder;
        private TextBlock statusText;
        private TextBlock wordCountText;

        // Toolbar Buttons
        private Button btnBold;
        private Button btnItalic;
        private Button btnUnderline;
        private Button btnBullet;
        private Button btnNumber;
        private Button btnIndent;
        private Button btnOutdent;
        private ComboBox previewModeCombo;

        // Table Picker Controls
        private Button btnTable;
        private System.Windows.Controls.Primitives.Popup tablePickerPopup;
        private Border[,] tablePickerCells;
        private TextBlock tablePickerLabel;
        private Border tablePickerPopupBorder;
        private const int TABLE_GRID_ROWS = 6;
        private const int TABLE_GRID_COLS = 8;

        // State Variables
        private bool isDarkMode = true; // Default to night mode
        private string nightTextColor = "White"; // Default text color
        private string currentFilePath = null;
        private bool isSyncing = false;
        private PreviewWindow externalPreviewWindow = null;

        public MainWindow()
        {
            InitializeWindow();
            CreateUI();
            ApplyTheme();
            LoadDefaultText();

            // Register text change handler
            visualEditor.TextChanged += VisualEditor_TextChanged;
        }

        private void InitializeWindow()
        {
            this.Title = "Nature to MD - Natural Text Markdown Editor";
            this.Width = 1200;
            this.Height = 780;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Handle close to clean up external window
            this.Closing += (s, e) => {
                if (externalPreviewWindow != null)
                {
                    externalPreviewWindow.Close();
                }
            };
        }

        private void CreateUI()
        {
            // Root Grid
            rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Menu & Info
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Editors & Preview
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status Bar
            this.Content = rootGrid;

            // Row 0: Quick Info & Top Header
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            TextBlock appLogo = new TextBlock
            {
                Text = "Nature to MD",
                FontFamily = new FontFamily("Segoe UI Semibold"),
                FontSize = 15,
                Margin = new Thickness(15, 8, 15, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerGrid.Children.Add(appLogo);

            StackPanel fileOps = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 15, 0) };
            Button btnNew = CreateStyledButton("New", (s, e) => ActionNew());
            Button btnOpen = CreateStyledButton("Open", (s, e) => ActionOpen());
            Button btnSave = CreateStyledButton("Save", (s, e) => ActionSave());
            Button btnCopyMd = CreateStyledButton("Copy MD", (s, e) => ActionCopyMarkdown());
            Button btnExportHtml = CreateStyledButton("Export HTML", (s, e) => ActionExportHtml());
            Button btnPrintPdf = CreateStyledButton("Print/PDF", (s, e) => ActionPrintPdf());
            
            fileOps.Children.Add(btnNew);
            fileOps.Children.Add(btnOpen);
            fileOps.Children.Add(btnSave);
            fileOps.Children.Add(btnCopyMd);
            fileOps.Children.Add(btnExportHtml);
            fileOps.Children.Add(btnPrintPdf);
            
            Grid.SetColumn(fileOps, 1);
            headerGrid.Children.Add(fileOps);
            rootGrid.Children.Add(headerGrid);

            // Row 1: Toolbar
            toolbarBorder = new Border { BorderThickness = new Thickness(0, 1, 0, 1), Padding = new Thickness(10, 5, 10, 5) };
            toolbar = new StackPanel { Orientation = Orientation.Horizontal };
            toolbarBorder.Child = toolbar;
            Grid.SetRow(toolbarBorder, 1);
            rootGrid.Children.Add(toolbarBorder);

            // Group 1: Headings Buttons
            Button btnH1 = CreateStyledButton("H1", (s, e) => SetHeading(1), "Heading 1");
            Button btnH2 = CreateStyledButton("H2", (s, e) => SetHeading(2), "Heading 2");
            Button btnH3 = CreateStyledButton("H3", (s, e) => SetHeading(3), "Heading 3");
            Button btnH4 = CreateStyledButton("H4", (s, e) => SetHeading(4), "Heading 4");
            Button btnNormal = CreateStyledButton("Normal", (s, e) => SetHeading(0), "Normal Text");
            Border grpHeadings = CreateGroupBorder(btnH1, btnH2, btnH3, btnH4, btnNormal);
            toolbar.Children.Add(grpHeadings);

            // Group 2: Inline Styles (Bold, Italic, Underline)
            btnBold = CreateStyledButton("B", (s, e) => {
                EditingCommands.ToggleBold.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Bold (Ctrl+B)", true);
            btnItalic = CreateStyledButton("I", (s, e) => {
                EditingCommands.ToggleItalic.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Italic (Ctrl+I)", false, true);
            btnUnderline = CreateStyledButton("U", (s, e) => {
                EditingCommands.ToggleUnderline.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Underline (Ctrl+U)", false, false, true);
            Border grpInline = CreateGroupBorder(btnBold, btnItalic, btnUnderline);
            toolbar.Children.Add(grpInline);

            // Group 3: Lists (Bullet, Numbered) - Microsoft Word Icons will be injected programmatically
            btnBullet = CreateStyledButton("", (s, e) => {
                EditingCommands.ToggleBullets.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Bullet List");
            btnNumber = CreateStyledButton("", (s, e) => {
                EditingCommands.ToggleNumbering.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Numbered List");
            Border grpLists = CreateGroupBorder(btnBullet, btnNumber);
            toolbar.Children.Add(grpLists);

            // Group 4: Indents (Indent, Outdent) - Microsoft Word Icons will be injected programmatically
            btnOutdent = CreateStyledButton("", (s, e) => {
                EditingCommands.DecreaseIndentation.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Decrease Indent (Shift+Tab)");
            btnIndent = CreateStyledButton("", (s, e) => {
                EditingCommands.IncreaseIndentation.Execute(null, visualEditor);
                visualEditor.Focus();
            }, "Increase Indent (Tab)");
            Border grpIndents = CreateGroupBorder(btnOutdent, btnIndent);
            toolbar.Children.Add(grpIndents);

            // Group 5: Table Insertion
            btnTable = CreateStyledButton("Table ▾", null, "Insert Table");
            btnTable.Click += BtnTable_Click;
            Border grpTable = CreateGroupBorder(btnTable);
            toolbar.Children.Add(grpTable);

            // Group 6: Settings (Theme & Text Color Dropdowns)
            themeLabel = new TextBlock { Text = "Theme: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 2, 0) };
            themeCombo = new ComboBox { Width = 110, Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Center };
            themeCombo.Items.Add("Night Mode");
            themeCombo.Items.Add("Light Mode");
            themeCombo.SelectedIndex = 0;
            themeCombo.SelectionChanged += ThemeCombo_SelectionChanged;

            textColorLabel = new TextBlock { Text = "Text: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0) };
            colorCombo = new ComboBox { Width = 90, Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Center };
            colorCombo.Items.Add("White");
            colorCombo.Items.Add("Green");
            colorCombo.Items.Add("Yellow");
            colorCombo.SelectedIndex = 0;
            colorCombo.SelectionChanged += ColorCombo_SelectionChanged;

            StackPanel settingsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            settingsPanel.Children.Add(themeLabel);
            settingsPanel.Children.Add(themeCombo);
            settingsPanel.Children.Add(textColorLabel);
            settingsPanel.Children.Add(colorCombo);
            Border grpSettings = CreateGroupBorder(settingsPanel);
            toolbar.Children.Add(grpSettings);

            // Group 6: Preview Mode Controls
            TextBlock previewLabel = new TextBlock { Text = "Preview: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 2, 0) };
            previewModeCombo = new ComboBox { Width = 130, Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Center };
            previewModeCombo.Items.Add("Split Pane");
            previewModeCombo.Items.Add("External Window");
            previewModeCombo.Items.Add("Editor Only");
            previewModeCombo.SelectedIndex = 0;
            previewModeCombo.SelectionChanged += PreviewModeCombo_SelectionChanged;
            
            StackPanel previewPanel = new StackPanel { Orientation = Orientation.Horizontal };
            previewPanel.Children.Add(previewLabel);
            previewPanel.Children.Add(previewModeCombo);
            Border grpPreview = CreateGroupBorder(previewPanel);
            toolbar.Children.Add(grpPreview);

            // Row 2: Content Grid
            mainContentGrid = new Grid();
            mainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Editor (Column 0)
            splitterColumn = new ColumnDefinition { Width = new GridLength(5) }; // Splitter (Column 1)
            previewColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }; // Preview (Column 2)
            mainContentGrid.ColumnDefinitions.Add(splitterColumn);
            mainContentGrid.ColumnDefinitions.Add(previewColumn);
            Grid.SetRow(mainContentGrid, 2);
            rootGrid.Children.Add(mainContentGrid);

            // Visual Editor Only (Column 0)
            Grid editorContainer = new Grid();
            
            visualEditor = new RichTextBox
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(30),
                BorderThickness = new Thickness(0),
                AcceptsTab = false // We handle Tab key manually for indenting
            };
            visualEditor.Document.PagePadding = new Thickness(10);
            visualEditor.Document.MaxPageWidth = 800;
            visualEditor.KeyDown += VisualEditor_KeyDown;
            visualEditor.SelectionChanged += VisualEditor_SelectionChanged;

            editorContainer.Children.Add(visualEditor);
            Grid.SetColumn(editorContainer, 0);
            mainContentGrid.Children.Add(editorContainer);

            // Splitter (Column 1)
            splitter = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 5,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            };
            Grid.SetColumn(splitter, 1);
            mainContentGrid.Children.Add(splitter);

            // WebBrowser Preview (Column 2)
            previewBrowser = new WebBrowser();
            Grid.SetColumn(previewBrowser, 2);
            mainContentGrid.Children.Add(previewBrowser);

            // Row 3: Status Bar
            statusBarBorder = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(15, 6, 15, 6) };
            Grid statusBarGrid = new Grid();
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusBarBorder.Child = statusBarGrid;

            statusText = new TextBlock { Text = "Ready" };
            Grid.SetColumn(statusText, 0);
            statusBarGrid.Children.Add(statusText);

            wordCountText = new TextBlock { Text = "Words: 0 | Chars: 0" };
            Grid.SetColumn(wordCountText, 1);
            statusBarGrid.Children.Add(wordCountText);

            Grid.SetRow(statusBarBorder, 3);
            rootGrid.Children.Add(statusBarBorder);
        }

        private Border CreateGroupBorder(params UIElement[] elements)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var el in elements)
            {
                panel.Children.Add(el);
            }
            
            Border border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(3),
                Margin = new Thickness(2, 0, 8, 0),
                Child = panel
            };
            
            return border;
        }

        private Button CreateStyledButton(string content, RoutedEventHandler handler, string tooltip = null, bool bold = false, bool italic = false, bool underline = false)
        {
            Button btn = new Button
            {
                Content = content,
                Margin = new Thickness(2),
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1)
            };
            if (handler != null)
            {
                btn.Click += handler;
            }
            
            if (tooltip != null) btn.ToolTip = tooltip;
            if (bold) btn.FontWeight = FontWeights.Bold;
            if (italic) btn.FontStyle = FontStyles.Italic;
            if (underline)
            {
                TextBlock tb = new TextBlock { Text = content, TextDecorations = TextDecorations.Underline };
                btn.Content = tb;
            }

            // Register hover handler ONCE
            btn.MouseEnter += (s, e) => {
                if (isDarkMode)
                {
                    // Hover in Night Mode: Lighter Purple
                    btn.Background = new SolidColorBrush(Color.FromRgb(159, 122, 234));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(159, 122, 234));
                    btn.Foreground = Brushes.White;
                }
                else
                {
                    // Hover in Light Mode: Soft gray/slate backdrop, transparent text changes to dark slate
                    btn.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                }
            };

            btn.MouseLeave += (s, e) => {
                ResetButtonColor(btn);
            };

            return btn;
        }

        private void ResetButtonColor(Button btn)
        {
            if (isDarkMode)
            {
                // Night mode: Purple background, white text, dark border
                btn.Background = new SolidColorBrush(Color.FromRgb(128, 90, 213));
                btn.Foreground = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 40, 80));
            }
            else
            {
                // Light mode: Transparent background, dark slate text, transparent border
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                btn.BorderBrush = Brushes.Transparent;
            }
        }

        private void ApplyTheme()
        {
            Color bg, panelBg, text, border, accent, editorBg, editorFg;
            
            if (isDarkMode)
            {
                bg = Color.FromRgb(0, 0, 0); // Pure Black Background
                panelBg = Color.FromRgb(15, 15, 20); // Dark Panel Background
                editorBg = Color.FromRgb(0, 0, 0); // Black Editor
                
                // Night mode text color settings
                if (nightTextColor == "Green")
                {
                    editorFg = Color.FromRgb(51, 255, 51); // Terminal Green
                }
                else if (nightTextColor == "Yellow")
                {
                    editorFg = Color.FromRgb(255, 204, 0); // Amber Yellow
                }
                else
                {
                    editorFg = Color.FromRgb(255, 255, 255); // White
                }

                text = Color.FromRgb(255, 255, 255); // Dashboard text remains white
                border = Color.FromRgb(50, 40, 80); // Purple border outline
                accent = Color.FromRgb(128, 90, 213); // Purple button theme color
            }
            else
            {
                bg = Color.FromRgb(248, 250, 252); // Light Slate
                panelBg = Color.FromRgb(241, 245, 249); // Warm Off-White
                editorBg = Color.FromRgb(255, 255, 255); // White Editor
                editorFg = Color.FromRgb(30, 41, 59); // Dark Slate Text
                
                text = Color.FromRgb(30, 41, 59); // Light Theme Text
                border = Color.FromRgb(226, 232, 240); // Soft grey border
                accent = Color.FromRgb(99, 102, 241); // Indigo button theme color
            }

            Brush bgBrush = new SolidColorBrush(bg);
            Brush panelBrush = new SolidColorBrush(panelBg);
            Brush textBrush = new SolidColorBrush(text);
            Brush borderBrush = new SolidColorBrush(border);
            Brush editorBgBrush = new SolidColorBrush(editorBg);
            Brush editorFgBrush = new SolidColorBrush(editorFg);

            // Apply MainWindow
            this.Background = bgBrush;
            this.Foreground = textBrush;

            // Apply Borders
            toolbarBorder.Background = panelBrush;
            toolbarBorder.BorderBrush = borderBrush;
            statusBarBorder.Background = panelBrush;
            statusBarBorder.BorderBrush = borderBrush;
            statusText.Foreground = textBrush;
            wordCountText.Foreground = textBrush;

            // Apply ComboBox dropdown styles — in night mode always use dark text on light bg for readability
            Brush comboFg, comboBg;
            if (isDarkMode)
            {
                comboFg = new SolidColorBrush(Color.FromRgb(20, 20, 20));   // Near-black text
                comboBg = new SolidColorBrush(Color.FromRgb(220, 215, 240)); // Soft lavender-grey background
            }
            else
            {
                comboFg = textBrush;  // Dark slate text
                comboBg = panelBrush; // Light off-white panel
            }
            ApplyComboBoxTheme(themeCombo, comboBg, comboFg, borderBrush);
            ApplyComboBoxTheme(colorCombo, comboBg, comboFg, borderBrush);
            ApplyComboBoxTheme(previewModeCombo, comboBg, comboFg, borderBrush);

            themeLabel.Foreground = textBrush;
            textColorLabel.Foreground = textBrush;

            // Apply Editors
            visualEditor.Background = editorBgBrush;
            visualEditor.Foreground = editorFgBrush;
            visualEditor.CaretBrush = editorFgBrush;

            // Update Word-style Vector Icons programmatically on theme change
            btnBullet.Content = IconFactory.CreateBulletListIcon(editorFgBrush);
            btnNumber.Content = IconFactory.CreateNumberedListIcon(editorFgBrush);
            btnOutdent.Content = IconFactory.CreateDecreaseIndentIcon(editorFgBrush);
            btnIndent.Content = IconFactory.CreateIncreaseIndentIcon(editorFgBrush);

            // Style buttons and groups in toolbar
            foreach (var child in toolbar.Children)
            {
                if (child is Border)
                {
                    Border groupBorder = (Border)child;
                    groupBorder.BorderBrush = borderBrush;
                    groupBorder.Background = panelBrush;

                    if (groupBorder.Child is StackPanel)
                    {
                        StackPanel groupPanel = (StackPanel)groupBorder.Child;
                        foreach (var btn in groupPanel.Children)
                        {
                            if (btn is Button)
                            {
                                ResetButtonColor((Button)btn);
                            }
                            else if (btn is StackPanel)
                            {
                                foreach (var sub in ((StackPanel)btn).Children)
                                {
                                    if (sub is TextBlock)
                                    {
                                        ((TextBlock)sub).Foreground = textBrush;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Also style top file operation buttons
            var topPanel = (StackPanel)((Grid)rootGrid.Children[0]).Children[1];
            foreach (var child in topPanel.Children)
            {
                if (child is Button)
                {
                    ResetButtonColor((Button)child);
                }
            }

            // Splitter color
            splitter.Background = borderBrush;

            // Update external window theme if active
            if (externalPreviewWindow != null && externalPreviewWindow.IsVisible)
            {
                externalPreviewWindow.UpdateThemeBackground(isDarkMode, editorBg);
            }

            // Refresh table picker popup theme if already created
            UpdateTablePickerPopupTheme();
        }

        // Apply ComboBox theme including the dropdown item list popup
        private void ApplyComboBoxTheme(ComboBox combo, Brush panelBrush, Brush textBrush, Brush borderBrush)
        {
            combo.Background = panelBrush;
            combo.Foreground = textBrush;
            combo.BorderBrush = borderBrush;

            // Create an ItemContainerStyle so that individual dropdown items
            // also get the correct foreground/background in the popup list.
            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, textBrush));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, panelBrush));
            combo.ItemContainerStyle = itemStyle;
        }

        // ==========================================
        // Table Picker Popup Logic
        // ==========================================

        private void BtnTable_Click(object sender, RoutedEventArgs e)
        {
            if (tablePickerPopup == null)
            {
                CreateTablePickerPopup();
            }
            UpdateTablePickerPopupTheme();
            tablePickerPopup.IsOpen = true;
        }

        private void CreateTablePickerPopup()
        {
            tablePickerPopup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = btnTable,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            tablePickerPopupBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12)
            };

            StackPanel popupContent = new StackPanel();

            tablePickerLabel = new TextBlock
            {
                Text = "Insert Table",
                FontFamily = new FontFamily("Segoe UI Semibold"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            popupContent.Children.Add(tablePickerLabel);

            Grid cellGrid = new Grid();
            tablePickerCells = new Border[TABLE_GRID_ROWS, TABLE_GRID_COLS];

            for (int r = 0; r < TABLE_GRID_ROWS; r++)
                cellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            for (int c = 0; c < TABLE_GRID_COLS; c++)
                cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            for (int r = 0; r < TABLE_GRID_ROWS; r++)
            {
                for (int c = 0; c < TABLE_GRID_COLS; c++)
                {
                    int row = r, col = c;
                    Border cell = new Border
                    {
                        BorderThickness = new Thickness(1.5),
                        Margin = new Thickness(2),
                        Width = 24,
                        Height = 24,
                        Cursor = Cursors.Hand
                    };
                    cell.MouseEnter += (s, ev) => UpdateTablePickerHover(row, col);
                    cell.MouseLeftButtonDown += (s, ev) => InsertMarkdownTable(row + 1, col + 1);
                    tablePickerCells[r, c] = cell;
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    cellGrid.Children.Add(cell);
                }
            }

            cellGrid.MouseLeave += (s, ev) => UpdateTablePickerHover(-1, -1);

            popupContent.Children.Add(cellGrid);
            tablePickerPopupBorder.Child = popupContent;
            tablePickerPopup.Child = tablePickerPopupBorder;
        }

        private void UpdateTablePickerHover(int hoverRow, int hoverCol)
        {
            if (tablePickerCells == null) return;

            Color selBorder, selBg, unselBorder, unselBg;

            if (isDarkMode)
            {
                if (nightTextColor == "Green")
                {
                    selBorder = Color.FromRgb(51, 255, 51);
                    selBg    = Color.FromArgb(40, 51, 255, 51);
                }
                else if (nightTextColor == "Yellow")
                {
                    selBorder = Color.FromRgb(255, 204, 0);
                    selBg    = Color.FromArgb(40, 255, 204, 0);
                }
                else // White
                {
                    selBorder = Color.FromRgb(255, 255, 255);
                    selBg    = Color.FromArgb(40, 255, 255, 255);
                }
                unselBorder = Color.FromRgb(80, 80, 80);
                unselBg     = Color.FromRgb(24, 20, 36);
            }
            else
            {
                selBorder   = Color.FromRgb(255, 0, 0); // Pure Red border for light mode
                selBg       = Color.FromArgb(20, 255, 0, 0); // Subtle red tint
                unselBorder = Color.FromRgb(100, 100, 100); // Darker gray border to match the screenshot
                unselBg     = Color.FromRgb(255, 255, 255); // White background to match the screenshot
            }

            for (int r = 0; r < TABLE_GRID_ROWS; r++)
            {
                for (int c = 0; c < TABLE_GRID_COLS; c++)
                {
                    bool sel = (r <= hoverRow && c <= hoverCol);
                    tablePickerCells[r, c].BorderBrush = new SolidColorBrush(sel ? selBorder : unselBorder);
                    tablePickerCells[r, c].Background  = new SolidColorBrush(sel ? selBg    : unselBg);
                }
            }

            if (hoverRow >= 0 && hoverCol >= 0)
                tablePickerLabel.Text = string.Format("{0} × {1} Table", hoverRow + 1, hoverCol + 1);
            else
                tablePickerLabel.Text = "Insert Table";
        }

        private void UpdateTablePickerPopupTheme()
        {
            if (tablePickerPopupBorder == null || tablePickerLabel == null) return;

            if (isDarkMode)
            {
                tablePickerPopupBorder.Background   = new SolidColorBrush(Color.FromRgb(18, 14, 30));
                tablePickerPopupBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(100, 70, 180));
                tablePickerPopupBorder.BorderThickness = new Thickness(1.5);
                tablePickerLabel.Foreground = Brushes.White;
            }
            else
            {
                tablePickerPopupBorder.Background   = Brushes.White;
                tablePickerPopupBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(200, 200, 215));
                tablePickerPopupBorder.BorderThickness = new Thickness(1);
                tablePickerLabel.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            }

            UpdateTablePickerHover(-1, -1);
        }

        private void InsertMarkdownTable(int rows, int cols)
        {
            if (tablePickerPopup != null) tablePickerPopup.IsOpen = false;

            System.Collections.Generic.List<string> tableLines = new System.Collections.Generic.List<string>();

            // Header row
            StringBuilder headerSb = new StringBuilder("|");
            for (int c = 1; c <= cols; c++) headerSb.AppendFormat(" Col {0} |", c);
            tableLines.Add(headerSb.ToString());

            // Separator row
            StringBuilder sepSb = new StringBuilder("|");
            for (int c = 0; c < cols; c++) sepSb.Append("---------|" );
            tableLines.Add(sepSb.ToString());

            // Data rows
            for (int r = 0; r < rows - 1; r++)
            {
                StringBuilder rowSb = new StringBuilder("|");
                for (int c = 0; c < cols; c++) rowSb.Append("         |");
                tableLines.Add(rowSb.ToString());
            }

            isSyncing = true;
            try
            {
                Paragraph caretParagraph = (visualEditor.CaretPosition != null)
                    ? visualEditor.CaretPosition.Paragraph : null;
                Block insertAfter = caretParagraph;

                if (insertAfter == null)
                {
                    foreach (string ln in tableLines)
                    {
                        Paragraph p = new Paragraph(new Run(ln));
                        p.FontFamily = new FontFamily("Consolas");
                        p.Margin = new Thickness(0, 1, 0, 1);
                        visualEditor.Document.Blocks.Add(p);
                    }
                }
                else
                {
                    foreach (string ln in tableLines)
                    {
                        Paragraph p = new Paragraph(new Run(ln));
                        p.FontFamily = new FontFamily("Consolas");
                        p.Margin = new Thickness(0, 1, 0, 1);
                        visualEditor.Document.Blocks.InsertAfter(insertAfter, p);
                        insertAfter = p;
                    }
                }

                UpdatePreviewContent();
            }
            finally
            {
                isSyncing = false;
            }

            visualEditor.Focus();
        }

        private void LoadDefaultText()
        {
            isSyncing = true;
            try
            {
                visualEditor.Document.Blocks.Clear();
                
                var h1 = new Paragraph(new Run("Nature to MD 快速指南"));
                h1.FontSize = 24;
                h1.FontWeight = FontWeights.Bold;
                visualEditor.Document.Blocks.Add(h1);

                var p1 = new Paragraph(new Run("歡迎使用 Nature to MD。這是一個原生 Windows 離線文書軟體，能將您輸入的自然格式文本轉換成 Markdown。"));
                visualEditor.Document.Blocks.Add(p1);

                var h2 = new Paragraph(new Run("基本文書功能"));
                h2.FontSize = 18;
                h2.FontWeight = FontWeights.Bold;
                visualEditor.Document.Blocks.Add(h2);

                var p2 = new Paragraph();
                p2.Inlines.Add(new Run("您可以使用工具列的按鈕，或者利用快速鍵設定 "));
                p2.Inlines.Add(new Bold(new Run("粗體 (Ctrl+B)")));
                p2.Inlines.Add(new Run("、"));
                p2.Inlines.Add(new Italic(new Run("斜體 (Ctrl+I)")));
                p2.Inlines.Add(new Run(" 與 "));
                p2.Inlines.Add(new Underline(new Run("下劃線")));
                p2.Inlines.Add(new Run("。"));
                visualEditor.Document.Blocks.Add(p2);

                var p3 = new Paragraph(new Run("縮排與清單功能："));
                visualEditor.Document.Blocks.Add(p3);

                var list = new List();
                list.MarkerStyle = TextMarkerStyle.Disc;
                list.ListItems.Add(new ListItem(new Paragraph(new Run("清單項目一（點擊工具列清單按鈕或編號按鈕來排列）"))));
                list.ListItems.Add(new ListItem(new Paragraph(new Run("您可以選取此行並點擊 Tab 鍵來進行縮排調整"))));
                
                var subList = new List();
                subList.MarkerStyle = TextMarkerStyle.Circle;
                subList.ListItems.Add(new ListItem(new Paragraph(new Run("這是第二層縮排清單"))));
                
                var subLi = new ListItem();
                list.ListItems.Add(subLi);
                subLi.Blocks.Add(subList);

                visualEditor.Document.Blocks.Add(list);

                var quote = new Paragraph(new Run("這是一段被縮排的區塊（點選縮排按鈕進行縮排會被視為 Blockquote 引言）。"));
                quote.Margin = new Thickness(20, 5, 0, 5);
                quote.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 90, 213));
                quote.BorderThickness = new Thickness(3, 0, 0, 0);
                quote.Padding = new Thickness(10, 0, 0, 0);
                visualEditor.Document.Blocks.Add(quote);

                UpdatePreviewContent();
            }
            finally
            {
                isSyncing = false;
            }
        }

        // ==========================================
        // Sync & Parsing Core Logic
        // ==========================================
        
        private void VisualEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSyncing) return;
            isSyncing = true;
            try
            {
                string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);
                UpdatePreviewContent();
                UpdateWordCounts(markdown);
            }
            finally
            {
                isSyncing = false;
            }
        }

        private void UpdatePreviewContent()
        {
            string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);
            string html = MarkdownConverter.MarkdownToHtml(markdown, isDarkMode, nightTextColor);

            // Update local splitter view
            if (previewBrowser.Visibility == Visibility.Visible)
            {
                try
                {
                    previewBrowser.NavigateToString(html);
                }
                catch
                {
                    // Catch random COM navigation issues
                }
            }

            // Update external window
            if (externalPreviewWindow != null && externalPreviewWindow.IsVisible)
            {
                externalPreviewWindow.UpdateContent(html);
            }
        }

        private void UpdateWordCounts(string text)
        {
            int chars = text.Length;
            int words = Regex.Matches(text, @"\b\w+\b").Count;
            int cjkCount = Regex.Matches(text, @"[\u4e00-\u9fa5]").Count;
            words += cjkCount;

            wordCountText.Text = string.Format("Words: {0} | Chars: {1}", words, chars);
        }

        // ==========================================
        // Dropdown Events
        // ==========================================

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (themeCombo == null || colorCombo == null) return;
            
            isDarkMode = themeCombo.SelectedIndex == 0;
            
            if (isDarkMode)
            {
                textColorLabel.IsEnabled = true;
                colorCombo.IsEnabled = true;
            }
            else
            {
                textColorLabel.IsEnabled = false;
                colorCombo.IsEnabled = false;
            }

            ApplyTheme();
            UpdatePreviewContent();
        }

        private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colorCombo == null) return;
            
            if (colorCombo.SelectedIndex == 0) nightTextColor = "White";
            else if (colorCombo.SelectedIndex == 1) nightTextColor = "Green";
            else if (colorCombo.SelectedIndex == 2) nightTextColor = "Yellow";

            ApplyTheme();
            UpdatePreviewContent();
        }

        // ==========================================
        // Toolbar Heading Actions
        // ==========================================

        private void SetHeading(int level)
        {
            if (visualEditor == null) return;
            
            TextSelection selection = visualEditor.Selection;
            if (selection == null) return;

            var start = selection.Start;
            var end = selection.End;
            TextPointer pointer = start;
            
            visualEditor.BeginChange();
            try
            {
                while (pointer != null && pointer.CompareTo(end) <= 0)
                {
                    if (pointer.Parent is Paragraph)
                    {
                        Paragraph p = (Paragraph)pointer.Parent;
                        ApplyHeadingToParagraph(p, level);
                    }
                    pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                }

                if (selection.Start.Paragraph != null)
                {
                    ApplyHeadingToParagraph(selection.Start.Paragraph, level);
                }
            }
            finally
            {
                visualEditor.EndChange();
                visualEditor.Focus();
                
                // Force update
                VisualEditor_TextChanged(null, null);
            }
        }

        private void ApplyHeadingToParagraph(Paragraph p, int level)
        {
            if (level == 0) // Normal text
            {
                p.FontSize = 12;
                p.FontWeight = FontWeights.Normal;
            }
            else if (level == 1) // H1
            {
                p.FontSize = 24;
                p.FontWeight = FontWeights.Bold;
            }
            else if (level == 2) // H2
            {
                p.FontSize = 18;
                p.FontWeight = FontWeights.Bold;
            }
            else if (level == 3) // H3
            {
                p.FontSize = 14;
                p.FontWeight = FontWeights.Bold;
            }
            else if (level == 4) // H4
            {
                p.FontSize = 12;
                p.FontWeight = FontWeights.Bold;
            }
        }

        private void VisualEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Empty selection change handler
        }

        private void VisualEditor_KeyDown(object sender, KeyEventArgs e)
        {
            // Custom Tab key behavior to support indents/outdents
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    EditingCommands.DecreaseIndentation.Execute(null, visualEditor);
                }
                else
                {
                    EditingCommands.IncreaseIndentation.Execute(null, visualEditor);
                }
            }
        }

        private void PreviewModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (previewModeCombo == null) return;

            int index = previewModeCombo.SelectedIndex;
            if (index == 0) // Split Pane
            {
                if (externalPreviewWindow != null)
                {
                    externalPreviewWindow.Close();
                    externalPreviewWindow = null;
                }

                splitterColumn.Width = new GridLength(5);
                previewColumn.Width = new GridLength(1, GridUnitType.Star);
                splitter.Visibility = Visibility.Visible;
                previewBrowser.Visibility = Visibility.Visible;
                
                UpdatePreviewContent();
            }
            else if (index == 1) // External Window
            {
                splitterColumn.Width = new GridLength(0);
                previewColumn.Width = new GridLength(0);
                splitter.Visibility = Visibility.Collapsed;
                previewBrowser.Visibility = Visibility.Collapsed;

                if (externalPreviewWindow == null)
                {
                    string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);
                    string html = MarkdownConverter.MarkdownToHtml(markdown, isDarkMode, nightTextColor);

                    Color editorBgColor = isDarkMode ? Color.FromRgb(0, 0, 0) : Color.FromRgb(255, 255, 255);

                    externalPreviewWindow = new PreviewWindow(html, isDarkMode, editorBgColor);
                    externalPreviewWindow.Closed += (s, ev) => {
                        if (previewModeCombo.SelectedIndex == 1)
                        {
                            previewModeCombo.SelectedIndex = 0;
                        }
                    };
                    externalPreviewWindow.Show();
                }
            }
            else if (index == 2) // Editor Only
            {
                if (externalPreviewWindow != null)
                {
                    externalPreviewWindow.Close();
                    externalPreviewWindow = null;
                }

                splitterColumn.Width = new GridLength(0);
                previewColumn.Width = new GridLength(0);
                splitter.Visibility = Visibility.Collapsed;
                previewBrowser.Visibility = Visibility.Collapsed;
            }
        }

        // ==========================================
        // File Actions & File Dialogs
        // ==========================================

        private void ActionNew()
        {
            if (MessageBox.Show("Are you sure you want to create a new document? Any unsaved changes will be lost.", "New Document", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                isSyncing = true;
                try
                {
                    visualEditor.Document.Blocks.Clear();
                    currentFilePath = null;
                    UpdateStatus("New document created.");
                    UpdatePreviewContent();
                }
                finally
                {
                    isSyncing = false;
                }
            }
        }

        private void ActionOpen()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                currentFilePath = ofd.FileName;
                
                isSyncing = true;
                try
                {
                    MarkdownConverter.MarkdownToFlowDocument(text, visualEditor.Document);
                    UpdatePreviewContent();
                    UpdateStatus("Opened: " + System.IO.Path.GetFileName(ofd.FileName));
                }
                finally
                {
                    isSyncing = false;
                }
            }
        }

        private void ActionSave()
        {
            string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);

            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "md"
                };

                if (sfd.ShowDialog() == true)
                {
                    currentFilePath = sfd.FileName;
                }
                else
                {
                    return;
                }
            }

            try
            {
                File.WriteAllText(currentFilePath, markdown, Encoding.UTF8);
                UpdateStatus("Saved successfully to " + System.IO.Path.GetFileName(currentFilePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActionCopyMarkdown()
        {
            try
            {
                string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);
                Clipboard.SetText(markdown);
                UpdateStatus("Markdown copied to clipboard!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error copying to clipboard: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActionExportHtml()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html",
                DefaultExt = "html",
                FileName = "ExportedDocument.html"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string markdown = MarkdownConverter.FlowDocumentToMarkdown(visualEditor.Document);
                    string html = MarkdownConverter.MarkdownToHtml(markdown, isDarkMode, nightTextColor);
                    File.WriteAllText(sfd.FileName, html, Encoding.UTF8);
                    UpdateStatus("Exported HTML: " + System.IO.Path.GetFileName(sfd.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting HTML: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ActionPrintPdf()
        {
            try
            {
                int currentMode = previewModeCombo.SelectedIndex;
                if (currentMode != 0)
                {
                    previewModeCombo.SelectedIndex = 0;
                }

                dynamic doc = previewBrowser.Document;
                if (doc != null)
                {
                    doc.execCommand("Print", true, null);
                    UpdateStatus("Print window triggered.");
                }
                else
                {
                    MessageBox.Show("Preview is not fully loaded. Please wait and try again.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error printing: " + ex.Message + "\nEnsure you are in Split Pane view and the preview is active.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string text)
        {
            statusText.Text = string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), text);
        }
    }

    public class PreviewWindow : Window
    {
        private WebBrowser browser;

        public PreviewWindow(string initialHtml, bool isDark, Color editorBg)
        {
            this.Title = "Markdown Real-time Preview - External Screen";
            this.Width = 800;
            this.Height = 600;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            browser = new WebBrowser();
            this.Content = browser;

            UpdateThemeBackground(isDark, editorBg);

            if (!string.IsNullOrEmpty(initialHtml))
            {
                UpdateContent(initialHtml);
            }
        }

        public void UpdateThemeBackground(bool isDark, Color editorBg)
        {
            this.Background = new SolidColorBrush(editorBg);
        }

        public void UpdateContent(string html)
        {
            try
            {
                browser.NavigateToString(html);
            }
            catch
            {
                // Catch dynamic COM browser error
            }
        }
    }

    // =========================================================================
    // Word-style Vector Icon Factory
    // =========================================================================
    public static class IconFactory
    {
        public static FrameworkElement CreateBulletListIcon(Brush lineBrush)
        {
            Canvas canvas = new Canvas { Width = 16, Height = 16 };
            Brush dotBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Word Blue
            
            for (int i = 0; i < 3; i++)
            {
                double y = 3.5 + i * 4.5;
                
                // Bullet dot
                Ellipse dot = new Ellipse { Width = 3, Height = 3, Fill = dotBrush };
                Canvas.SetLeft(dot, 1);
                Canvas.SetTop(dot, y - 1.5);
                canvas.Children.Add(dot);
                
                // Horizontal line
                Line line = new Line { X1 = 6, Y1 = y, X2 = 15, Y2 = y, Stroke = lineBrush, StrokeThickness = 1.2 };
                canvas.Children.Add(line);
            }
            return canvas;
        }

        public static FrameworkElement CreateNumberedListIcon(Brush lineBrush)
        {
            Canvas canvas = new Canvas { Width = 16, Height = 16 };
            Brush numBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Word Blue
            
            string[] nums = { "1", "2", "3" };
            for (int i = 0; i < 3; i++)
            {
                double y = 3.5 + i * 4.5;
                
                // Number label
                TextBlock tb = new TextBlock
                {
                    Text = nums[i],
                    FontSize = 7,
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    Foreground = numBrush
                };
                Canvas.SetLeft(tb, 0);
                Canvas.SetTop(tb, y - 4.5);
                canvas.Children.Add(tb);
                
                // Horizontal line
                Line line = new Line { X1 = 7, Y1 = y, X2 = 15, Y2 = y, Stroke = lineBrush, StrokeThickness = 1.2 };
                canvas.Children.Add(line);
            }
            return canvas;
        }

        public static FrameworkElement CreateDecreaseIndentIcon(Brush lineBrush)
        {
            Canvas canvas = new Canvas { Width = 16, Height = 16 };
            Brush arrowBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Word Blue
            
            // Top full horizontal line
            canvas.Children.Add(new Line { X1 = 0, Y1 = 2, X2 = 15, Y2 = 2, Stroke = lineBrush, StrokeThickness = 1.2 });
            // Bottom full horizontal line
            canvas.Children.Add(new Line { X1 = 0, Y1 = 14, X2 = 15, Y2 = 14, Stroke = lineBrush, StrokeThickness = 1.2 });
            
            // Middle indented lines
            canvas.Children.Add(new Line { X1 = 7, Y1 = 5, X2 = 15, Y2 = 5, Stroke = lineBrush, StrokeThickness = 1.2 });
            canvas.Children.Add(new Line { X1 = 7, Y1 = 8, X2 = 15, Y2 = 8, Stroke = lineBrush, StrokeThickness = 1.2 });
            canvas.Children.Add(new Line { X1 = 7, Y1 = 11, X2 = 15, Y2 = 11, Stroke = lineBrush, StrokeThickness = 1.2 });
            
            // Blue left-pointing arrow
            System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path
            {
                Stroke = arrowBrush,
                StrokeThickness = 1.5,
                Data = Geometry.Parse("M 4,5 L 1,8 L 4,11 M 1,8 L 5,8")
            };
            canvas.Children.Add(arrow);
            return canvas;
        }

        public static FrameworkElement CreateIncreaseIndentIcon(Brush lineBrush)
        {
            Canvas canvas = new Canvas { Width = 16, Height = 16 };
            Brush arrowBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Word Blue
            
            // Top full horizontal line
            canvas.Children.Add(new Line { X1 = 0, Y1 = 2, X2 = 15, Y2 = 2, Stroke = lineBrush, StrokeThickness = 1.2 });
            // Bottom full horizontal line
            canvas.Children.Add(new Line { X1 = 0, Y1 = 14, X2 = 15, Y2 = 14, Stroke = lineBrush, StrokeThickness = 1.2 });
            
            // Middle indented lines
            canvas.Children.Add(new Line { X1 = 7, Y1 = 5, X2 = 15, Y2 = 5, Stroke = lineBrush, StrokeThickness = 1.2 });
            canvas.Children.Add(new Line { X1 = 7, Y1 = 8, X2 = 15, Y2 = 8, Stroke = lineBrush, StrokeThickness = 1.2 });
            canvas.Children.Add(new Line { X1 = 7, Y1 = 11, X2 = 15, Y2 = 11, Stroke = lineBrush, StrokeThickness = 1.2 });
            
            // Blue right-pointing arrow
            System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path
            {
                Stroke = arrowBrush,
                StrokeThickness = 1.5,
                Data = Geometry.Parse("M 1,5 L 4,8 L 1,11 M 1,8 L 4,8")
            };
            canvas.Children.Add(arrow);
            return canvas;
        }
    }

    // =========================================================================
    // Markdown Converter Engine (Pure C# Standard 100% Offline Parser)
    // =========================================================================
    
    public static class MarkdownConverter
    {
        public static string FlowDocumentToMarkdown(FlowDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block block in doc.Blocks)
            {
                sb.Append(BlockToMarkdown(block));
            }
            return sb.ToString().TrimEnd() + "\n";
        }

        private static string BlockToMarkdown(Block block)
        {
            if (block is Paragraph)
            {
                Paragraph p = (Paragraph)block;
                StringBuilder sb = new StringBuilder();

                string prefix = "";
                if (p.FontSize >= 24) prefix = "# ";
                else if (p.FontSize >= 18) prefix = "## ";
                else if (p.FontSize >= 14) prefix = "### ";
                else if (p.FontSize == 12 && p.FontWeight == FontWeights.Bold) prefix = "#### ";

                sb.Append(prefix);

                int indentLevel = (int)(p.Margin.Left / 20);
                if (indentLevel > 0 && !(p.Parent is ListItem))
                {
                    string quotePrefix = "";
                    for (int i = 0; i < indentLevel; i++) quotePrefix += "> ";
                    sb.Append(quotePrefix);
                }

                foreach (Inline inline in p.Inlines)
                {
                    sb.Append(InlineToMarkdown(inline));
                }

                // Use single newline for table rows to preserve contiguous structure
                string rowText = sb.ToString();
                if (rowText.TrimStart().StartsWith("|"))
                    sb.Append("\n");
                else
                    sb.Append("\n\n");
                return sb.ToString();
            }
            else if (block is List)
            {
                List list = (List)block;
                StringBuilder sb = new StringBuilder();
                int index = 1;
                
                foreach (ListItem item in list.ListItems)
                {
                    string listPrefix = list.MarkerStyle == TextMarkerStyle.Decimal ? (index++ + ". ") : "- ";
                    
                    foreach (Block itemBlock in item.Blocks)
                    {
                        if (itemBlock is Paragraph)
                        {
                            Paragraph ip = (Paragraph)itemBlock;
                            sb.Append(listPrefix);
                            foreach (Inline inline in ip.Inlines)
                            {
                                sb.Append(InlineToMarkdown(inline));
                            }
                            sb.Append("\n");
                        }
                        else if (itemBlock is List)
                        {
                            string nested = BlockToMarkdown(itemBlock);
                            string[] nestedLines = nested.Split(new[] { "\n" }, StringSplitOptions.None);
                            foreach (string line in nestedLines)
                            {
                                if (!string.IsNullOrEmpty(line))
                                {
                                    sb.Append("    " + line + "\n");
                                }
                            }
                        }
                    }
                }
                sb.Append("\n");
                return sb.ToString();
            }
            else if (block is Section)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Block subBlock in ((Section)block).Blocks)
                {
                    sb.Append(BlockToMarkdown(subBlock));
                }
                return sb.ToString();
            }
            return "";
        }

        private static string InlineToMarkdown(Inline inline)
        {
            if (inline is Run)
            {
                return ((Run)inline).Text;
            }
            else if (inline is Bold)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("**");
                foreach (Inline subInline in ((Bold)inline).Inlines)
                {
                    sb.Append(InlineToMarkdown(subInline));
                }
                sb.Append("**");
                return sb.ToString();
            }
            else if (inline is Italic)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("*");
                foreach (Inline subInline in ((Italic)inline).Inlines)
                {
                    sb.Append(InlineToMarkdown(subInline));
                }
                sb.Append("*");
                return sb.ToString();
            }
            else if (inline is Underline)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<u>");
                foreach (Inline subInline in ((Underline)inline).Inlines)
                {
                    sb.Append(InlineToMarkdown(subInline));
                }
                sb.Append("</u>");
                return sb.ToString();
            }
            else if (inline is Hyperlink)
            {
                Hyperlink link = (Hyperlink)inline;
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                foreach (Inline subInline in link.Inlines)
                {
                    sb.Append(InlineToMarkdown(subInline));
                }
                sb.Append("](" + link.NavigateUri + ")");
                return sb.ToString();
            }
            else if (inline is LineBreak)
            {
                return "\n";
            }
            else if (inline is Span)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Inline subInline in ((Span)inline).Inlines)
                {
                    sb.Append(InlineToMarkdown(subInline));
                }
                return sb.ToString();
            }
            return "";
        }

        public static void MarkdownToFlowDocument(string markdown, FlowDocument doc)
        {
            doc.Blocks.Clear();
            string[] lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("# "))
                {
                    Paragraph p = CreateFormattedParagraph(line.Substring(2));
                    p.FontSize = 24;
                    p.FontWeight = FontWeights.Bold;
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith("## "))
                {
                    Paragraph p = CreateFormattedParagraph(line.Substring(3));
                    p.FontSize = 18;
                    p.FontWeight = FontWeights.Bold;
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith("### "))
                {
                    Paragraph p = CreateFormattedParagraph(line.Substring(4));
                    p.FontSize = 14;
                    p.FontWeight = FontWeights.Bold;
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith("#### "))
                {
                    Paragraph p = CreateFormattedParagraph(line.Substring(5));
                    p.FontSize = 12;
                    p.FontWeight = FontWeights.Bold;
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith(">"))
                {
                    int depth = 0;
                    while (depth < line.Length && line[depth] == '>') depth++;
                    string content = depth < line.Length ? line.Substring(depth).TrimStart() : "";
                    
                    Paragraph p = CreateFormattedParagraph(content);
                    p.Margin = new Thickness(20 * depth, 5, 0, 5);
                    p.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 90, 213));
                    p.BorderThickness = new Thickness(3, 0, 0, 0);
                    p.Padding = new Thickness(10, 0, 0, 0);
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    List list = new List { MarkerStyle = TextMarkerStyle.Disc };
                    while (i < lines.Length && (lines[i].StartsWith("- ") || lines[i].StartsWith("* ")))
                    {
                        string content = lines[i].Substring(2);
                        ListItem li = new ListItem();
                        li.Blocks.Add(CreateFormattedParagraph(content));
                        list.ListItems.Add(li);
                        i++;
                    }
                    i--;
                    doc.Blocks.Add(list);
                }
                else if (Regex.IsMatch(line, @"^\d+\.\s"))
                {
                    List list = new List { MarkerStyle = TextMarkerStyle.Decimal };
                    while (i < lines.Length && Regex.IsMatch(lines[i], @"^\d+\.\s"))
                    {
                        int dotIndex = lines[i].IndexOf('.');
                        string content = lines[i].Substring(dotIndex + 2);
                        ListItem li = new ListItem();
                        li.Blocks.Add(CreateFormattedParagraph(content));
                        list.ListItems.Add(li);
                        i++;
                    }
                    i--;
                    doc.Blocks.Add(list);
                }
                else
                {
                    Paragraph p = CreateFormattedParagraph(line);
                    doc.Blocks.Add(p);
                }
            }
        }

        private static Paragraph CreateFormattedParagraph(string text)
        {
            Paragraph p = new Paragraph { Margin = new Thickness(0, 5, 0, 5) };
            
            string pattern = @"(\*\*.*?\*\*|\*.*?\*|<u>.*?</u>|\[.*?\]\(.*?\))";
            string[] parts = Regex.Split(text, pattern);

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("**") && part.EndsWith("**"))
                {
                    string inner = part.Substring(2, part.Length - 4);
                    p.Inlines.Add(new Bold(new Run(inner)));
                }
                else if (part.StartsWith("*") && part.EndsWith("*"))
                {
                    string inner = part.Substring(1, part.Length - 2);
                    p.Inlines.Add(new Italic(new Run(inner)));
                }
                else if (part.StartsWith("<u>") && part.EndsWith("</u>"))
                {
                    string inner = part.Substring(3, part.Length - 7);
                    p.Inlines.Add(new Underline(new Run(inner)));
                }
                else if (part.StartsWith("[") && part.Contains("]("))
                {
                    int closeBracket = part.IndexOf(']');
                    string linkText = part.Substring(1, closeBracket - 1);
                    string url = part.Substring(closeBracket + 2, part.Length - closeBracket - 3);
                    try
                    {
                        Hyperlink link = new Hyperlink(new Run(linkText));
                        link.NavigateUri = new Uri(url);
                        p.Inlines.Add(link);
                    }
                    catch
                    {
                        p.Inlines.Add(new Run(part));
                    }
                }
                else
                {
                    p.Inlines.Add(new Run(part));
                }
            }

            return p;
        }

        public static string MarkdownToHtml(string markdown, bool isDark, string nightColor)
        {
            StringBuilder sb = new StringBuilder();

            // Set up HTML document structure with modern responsive styles and IE edge meta tag
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            sb.AppendLine("<meta http-equiv='X-UA-Compatible' content='IE=edge' />");
            sb.AppendLine("<style>");
            
            if (isDark)
            {
                string textHex = "#ffffff";
                string codeHex = "#f472b6";
                string borderHex = "#334155";
                string quoteBorderHex = "#805ad5";
                string quoteTextHex = "#94a3b8";
                string linkHex = "#a78bfa";
                string codeBgHex = "#1a1a24";

                if (nightColor == "Green")
                {
                    textHex = "#33ff33";
                    codeHex = "#88ff88";
                    borderHex = "#004400";
                    quoteBorderHex = "#00aa00";
                    quoteTextHex = "#22aa22";
                    linkHex = "#55ff55";
                    codeBgHex = "#001100";
                }
                else if (nightColor == "Yellow")
                {
                    textHex = "#ffcc00";
                    codeHex = "#ffdd44";
                    borderHex = "#442200";
                    quoteBorderHex = "#ffaa00";
                    quoteTextHex = "#ccaa00";
                    linkHex = "#ffbb33";
                    codeBgHex = "#140a00";
                }

                sb.AppendLine("body { background-color: #000000; color: " + textHex + "; font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; padding: 25px; margin: 0; }");
                sb.AppendLine("h1, h2, h3, h4 { color: " + textHex + "; border-bottom: 1px solid " + borderHex + "; padding-bottom: 8px; margin-top: 24px; margin-bottom: 12px; }");
                sb.AppendLine("h1 { font-size: 1.8em; } h2 { font-size: 1.5em; } h3 { font-size: 1.25em; } h4 { font-size: 1.1em; }");
                sb.AppendLine("p { margin-top: 0; margin-bottom: 16px; }");
                sb.AppendLine("code { background-color: " + codeBgHex + "; padding: 2px 6px; border-radius: 4px; font-family: Consolas, monospace; font-size: 0.9em; color: " + codeHex + "; border: 1px solid " + borderHex + "; }");
                sb.AppendLine("pre { background-color: " + codeBgHex + "; padding: 15px; border-radius: 8px; overflow-x: auto; border: 1px solid " + borderHex + "; margin-bottom: 16px; }");
                sb.AppendLine("pre code { background-color: transparent; padding: 0; color: " + textHex + "; border: none; }");
                sb.AppendLine("blockquote { border-left: 4px solid " + quoteBorderHex + "; margin: 0 0 16px 0; padding-left: 16px; color: " + quoteTextHex + "; font-style: italic; }");
                sb.AppendLine("a { color: " + linkHex + "; text-decoration: none; }");
                sb.AppendLine("a:hover { text-decoration: underline; }");
                sb.AppendLine("ul, ol { margin-top: 0; margin-bottom: 16px; padding-left: 24px; }");
                sb.AppendLine("li { margin-bottom: 4px; }");
                sb.AppendLine("hr { border: 0; border-top: 1px solid " + borderHex + "; margin: 24px 0; }");
                sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 16px; }");
                sb.AppendLine("th, td { border: 1px solid " + borderHex + "; padding: 8px 14px; text-align: left; }");
                sb.AppendLine("th { background-color: " + codeBgHex + "; font-weight: 600; color: " + textHex + "; }");
                sb.AppendLine("tr:nth-child(even) td { background-color: " + codeBgHex + "; }");
            }
            else
            {
                sb.AppendLine("body { background-color: #ffffff; color: #1e293b; font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; padding: 25px; margin: 0; }");
                sb.AppendLine("h1, h2, h3, h4 { color: #0f172a; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; margin-top: 24px; margin-bottom: 12px; }");
                sb.AppendLine("h1 { font-size: 1.8em; } h2 { font-size: 1.5em; } h3 { font-size: 1.25em; } h4 { font-size: 1.1em; }");
                sb.AppendLine("p { margin-top: 0; margin-bottom: 16px; }");
                sb.AppendLine("code { background-color: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-family: Consolas, monospace; font-size: 0.9em; color: #db2777; }");
                sb.AppendLine("pre { background-color: #f8fafc; padding: 15px; border-radius: 8px; overflow-x: auto; border: 1px solid #e2e8f0; margin-bottom: 16px; }");
                sb.AppendLine("pre code { background-color: transparent; padding: 0; color: #1e293b; }");
                sb.AppendLine("blockquote { border-left: 4px solid #6366f1; margin: 0 0 16px 0; padding-left: 16px; color: #64748b; font-style: italic; }");
                sb.AppendLine("a { color: #4f46e5; text-decoration: none; }");
                sb.AppendLine("a:hover { text-decoration: underline; }");
                sb.AppendLine("ul, ol { margin-top: 0; margin-bottom: 16px; padding-left: 24px; }");
                sb.AppendLine("li { margin-bottom: 4px; }");
                sb.AppendLine("hr { border: 0; border-top: 1px solid #e2e8f0; margin: 24px 0; }");
                sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 16px; }");
                sb.AppendLine("th, td { border: 1px solid #cbd5e1; padding: 8px 14px; text-align: left; }");
                sb.AppendLine("th { background-color: #f1f5f9; font-weight: 600; color: #0f172a; }");
                sb.AppendLine("tr:nth-child(even) td { background-color: #f8fafc; }");
            }
            sb.AppendLine("</style></head><body>");

            string[] lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inList = false;
            bool inNumList = false;
            bool inCode = false;
            bool inQuote = false;
            int quoteDepth = 0;
            bool inTable = false;
            bool tableHeaderDone = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.StartsWith("```"))
                {
                    if (inCode)
                    {
                        sb.AppendLine("</code></pre>");
                        inCode = false;
                    }
                    else
                    {
                        sb.AppendLine("<pre><code>");
                        inCode = true;
                    }
                    continue;
                }

                if (inCode)
                {
                    sb.AppendLine(EscapeHtml(line));
                    continue;
                }

                // Close any open table when this line is not a table row
                if (inTable && !line.TrimStart().StartsWith("|"))
                {
                    sb.AppendLine("</tbody></table>");
                    inTable = false;
                    tableHeaderDone = false;
                }

                if (line.StartsWith("# "))
                {
                    CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                    sb.AppendLine("<h1>" + ParseInlineFormatting(line.Substring(2)) + "</h1>");
                    continue;
                }
                if (line.StartsWith("## "))
                {
                    CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                    sb.AppendLine("<h2>" + ParseInlineFormatting(line.Substring(3)) + "</h2>");
                    continue;
                }
                if (line.StartsWith("### "))
                {
                    CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                    sb.AppendLine("<h3>" + ParseInlineFormatting(line.Substring(4)) + "</h3>");
                    continue;
                }
                if (line.StartsWith("#### "))
                {
                    CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                    sb.AppendLine("<h4>" + ParseInlineFormatting(line.Substring(5)) + "</h4>");
                    continue;
                }

                if (line.StartsWith(">"))
                {
                    CloseListOnly(sb, ref inList, ref inNumList);
                    int depth = 0;
                    while (depth < line.Length && line[depth] == '>') depth++;
                    string content = depth < line.Length ? line.Substring(depth).TrimStart() : "";

                    if (!inQuote || depth > quoteDepth)
                    {
                        for (int d = quoteDepth; d < depth; d++)
                        {
                            sb.AppendLine("<blockquote>");
                        }
                        inQuote = true;
                        quoteDepth = depth;
                    }
                    else if (depth < quoteDepth)
                    {
                        for (int d = depth; d < quoteDepth; d++)
                        {
                            sb.AppendLine("</blockquote>");
                        }
                        quoteDepth = depth;
                    }
                    
                    sb.AppendLine("<p>" + ParseInlineFormatting(content) + "</p>");
                    continue;
                }
                else if (inQuote && string.IsNullOrWhiteSpace(line))
                {
                    for (int d = 0; d < quoteDepth; d++)
                    {
                        sb.AppendLine("</blockquote>");
                    }
                    inQuote = false;
                    quoteDepth = 0;
                }

                // Table row detection
                bool isTableRow = !string.IsNullOrWhiteSpace(line) && line.TrimStart().StartsWith("|");
                bool isSeparatorRow = isTableRow && Regex.IsMatch(line.Replace(" ", ""), @"^\|[-:|]+(\|[-:|]+)*\|$");

                if (isTableRow)
                {
                    CloseListOnly(sb, ref inList, ref inNumList);
                    if (!inTable && !isSeparatorRow)
                    {
                        sb.AppendLine("<table>");
                        sb.AppendLine("<thead><tr>");
                        string[] hCells = line.Trim().Trim('|').Split('|');
                        foreach (string hc in hCells)
                            sb.AppendLine("<th>" + ParseInlineFormatting(hc.Trim()) + "</th>");
                        sb.AppendLine("</tr>");
                        inTable = true;
                        tableHeaderDone = false;
                    }
                    else if (inTable && !tableHeaderDone && isSeparatorRow)
                    {
                        sb.AppendLine("</thead><tbody>");
                        tableHeaderDone = true;
                    }
                    else if (inTable && tableHeaderDone && !isSeparatorRow)
                    {
                        sb.AppendLine("<tr>");
                        string[] dCells = line.Trim().Trim('|').Split('|');
                        foreach (string dc in dCells)
                            sb.AppendLine("<td>" + ParseInlineFormatting(dc.Trim()) + "</td>");
                        sb.AppendLine("</tr>");
                    }
                    continue;
                }

                bool isBullet = line.StartsWith("- ") || line.StartsWith("* ");
                bool isNumList = !isBullet && Regex.IsMatch(line, @"^\d+\.\s");

                if (isBullet)
                {
                    CloseNumberedListOnly(sb, ref inNumList);
                    if (!inList)
                    {
                        sb.AppendLine("<ul>");
                        inList = true;
                    }
                    sb.AppendLine("<li>" + ParseInlineFormatting(line.Substring(2)) + "</li>");
                    continue;
                }
                else if (isNumList)
                {
                    CloseBulletListOnly(sb, ref inList);
                    if (!inNumList)
                    {
                        sb.AppendLine("<ol>");
                        inNumList = true;
                    }
                    int dotIdx = line.IndexOf('.');
                    sb.AppendLine("<li>" + ParseInlineFormatting(line.Substring(dotIdx + 2)) + "</li>");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                    continue;
                }

                CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
                sb.AppendLine("<p>" + ParseInlineFormatting(line) + "</p>");
            }

            if (inTable) { sb.AppendLine("</tbody></table>"); }
            CloseListAndQuotes(sb, ref inList, ref inNumList, ref inQuote, ref quoteDepth);
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static void CloseListAndQuotes(StringBuilder sb, ref bool inList, ref bool inNumList, ref bool inQuote, ref int quoteDepth)
        {
            CloseListOnly(sb, ref inList, ref inNumList);
            if (inQuote)
            {
                for (int d = 0; d < quoteDepth; d++)
                {
                    sb.AppendLine("</blockquote>");
                }
                inQuote = false;
                quoteDepth = 0;
            }
        }

        private static void CloseListOnly(StringBuilder sb, ref bool inList, ref bool inNumList)
        {
            CloseBulletListOnly(sb, ref inList);
            CloseNumberedListOnly(sb, ref inNumList);
        }

        private static void CloseBulletListOnly(StringBuilder sb, ref bool inList)
        {
            if (inList)
            {
                sb.AppendLine("</ul>");
                inList = false;
            }
        }

        private static void CloseNumberedListOnly(StringBuilder sb, ref bool inNumList)
        {
            if (inNumList)
            {
                sb.AppendLine("</ol>");
                inNumList = false;
            }
        }

        private static string EscapeHtml(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string ParseInlineFormatting(string text)
        {
            text = EscapeHtml(text);
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<strong>$1</strong>");
            text = Regex.Replace(text, @"\*(.*?)\*", "<em>$1</em>");
            text = text.Replace("&lt;u&gt;", "<u>").Replace("&lt;/u&gt;", "</u>");
            text = Regex.Replace(text, @"`(.*?)`", "<code>$1</code>");
            text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "<a href='$2' target='_blank'>$1</a>");
            text = Regex.Replace(text, @"\!\[(.*?)\]\((.*?)\)", "<img src='$2' alt='$1' style='max-width:100%; height:auto;'>");
            return text;
        }
    }
}
