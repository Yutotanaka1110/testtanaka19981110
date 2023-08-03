using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO; // 追加が必要
using Microsoft.Win32;  


namespace TestTanaka19981110
{
    public partial class Form1 : Form
    {
        private Dictionary<int, string> tabPaths = new Dictionary<int, string>();
        private Dictionary<int, Tuple<int, int, int>> tabColors = new Dictionary<int, Tuple<int, int, int>>();
        private bool isEditing = false;
        private string originalFileName = "";
        private string pathBeforeChanged = "";
        private Stopwatch stopwatch = new Stopwatch();
        private bool isDrag = true ;
        private bool isDragging = false;
        private SortOrder sortOrder = SortOrder.None;


        int x0 = 0;
        int y0 = 108;
        int w = 673;
        int h = 450;

        public Form1()
        {
            InitializeComponent();

            // ファイルから色情報を読み込む
            LoadTabColorsFromFile();

            tabControl1.AllowDrop = true;
            tabControl1.DragEnter += tabControl1_DragEnter;
            tabControl1.DragDrop += tabControl1_DragDrop;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(tabControl1_DrawItem);
            tabControl1.MouseClick += tabControl1_MouseClick;
            tabControl1.Appearance = TabAppearance.Normal;
            tabControl1.BackColor = Color.Black;

            if (File.Exists("tabs.txt"))
            {
                using (StreamReader reader = new StreamReader("tabs.txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] tabInfo = line.Split('\t');
                        if (tabInfo.Length == 2 && int.TryParse(tabInfo[0], out int tabIndex))
                        {
                            string tabPath = tabInfo[1];
                            if (!tabPaths.ContainsKey(tabIndex)) // 同じキーが存在しない場合のみ追加する
                            {
                                //tabPaths.Add(tabIndex, tabPath);
                                AddSelectedPath(tabPath); // タブを作成するメソッドを呼び出す
                            }
                        }
                    }
                }
            }


            // ファイルから色情報を読み込む
            LoadTabColorsFromFile();


            // フォームのコンストラクタに追加するコード
            fileListView.SmallImageList = new ImageList();
            fileListView.SmallImageList.ImageSize = new Size(16, 16); // アイコンのサイズを設定（適宜調整可能）
            fileListView.SmallImageList.ColorDepth = ColorDepth.Depth32Bit; // カラーデプスを設定（適宜調整可能）

            fileListView.LargeImageList = new ImageList();
            fileListView.LargeImageList.ImageSize = new Size(32, 32); // アイコンのサイズを設定（適宜調整可能）
            fileListView.LargeImageList.ColorDepth = ColorDepth.Depth32Bit; // カラーデプスを設定（適宜調整可能）

            fileListView.SmallImageList = new ImageList();
            fileListView.SmallImageList.ImageSize = new Size(16, 16); // アイコンのサイズを設定（適宜調整可能）
            fileListView.SmallImageList.ColorDepth = ColorDepth.Depth32Bit; // カラーデプスを設定（適宜調整可能）

            // カラム幅の調整
            fileListView.Columns[0].Width = 300;
            fileListView.Columns[1].Width = 70;
            fileListView.Columns[2].Width = 150;
            fileListView.Columns[3].Width = 150;
            fileListView.AllowDrop = true; // ドラッグ＆ドロップを許可する
                                           
            fileListView.LabelEdit = true; // LabelEditプロパティをtrueに設定

            // コンストラクタ内に追加
            fileListView.DoubleClick += fileListView_DoubleClick;

            //fileListView.MouseDown += new MouseEventHandler(fileListView_MouseDown);
            //fileListView.DragEnter += new DragEventHandler(fileListView_DragEnter);
            //fileListView.DragDrop += new DragEventHandler(fileListView_DragDrop);
            // マウス移動イベントハンドラを設定
            fileListView.MouseDown += new MouseEventHandler(fileListView_MouseDown);
            fileListView.DragEnter += new DragEventHandler(fileListView_DragEnter);
            fileListView.DragDrop += new DragEventHandler(fileListView_DragDrop);
            fileListView.AfterLabelEdit += fileListView_AfterLabelEdit;
            fileListView.BeforeLabelEdit += fileListView_BeforeLabelEdit;
            fileListView.DrawColumnHeader += fileListView_DrawColumnHeader;


            addressBar.Enter += addressBar_Enter;
            addressBar.Click += addressBar_Click;

            // デフォルトのパスを設定して表示
            //string defaultPath = @"C:\Users\81905\tanakas\kyousin";
            //addressBar.Text = defaultPath;
            //tabPaths.Add(0, defaultPath);
            //AddTab();
            //UpdateTabName();
            //ShowFileList(defaultPath);
        }

        // Color.LightCyanをTuple<int, int, int>形式に変換するヘルパーメソッド
        private Tuple<int, int, int> ColorToTuple(Color color)
        {
            return Tuple.Create((int)(color.R), (int)(color.G), (int)(color.B));
        }

        private Color TupleToColor(Tuple<int, int, int> tuple)
        {
            return Color.FromArgb(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        private void SaveTabColorsToFile()
        {
            using (StreamWriter writer = new StreamWriter("tab_colors.txt"))
            {
                foreach (var entry in tabColors)
                {
                    int tabIndex = entry.Key;
                    int r = entry.Value.Item1;
                    int g = entry.Value.Item2;
                    int b = entry.Value.Item3;
                    writer.WriteLine($"{tabIndex},{r},{g},{b}");
                }
            }
        }

        private void LoadTabColorsFromFile()
        {
            if (File.Exists("tab_colors.txt"))
            {
                string[] lines = File.ReadAllLines("tab_colors.txt");
                tabColors.Clear();
                foreach (string line in lines)
                {
                    string[] components = line.Split(',');
                    if (components.Length == 4 && int.TryParse(components[0], out int tabIndex) && int.TryParse(components[1], out int r) && int.TryParse(components[2], out int g) && int.TryParse(components[3], out int b))
                    {
                        tabColors[tabIndex] = Tuple.Create(r, g, b);
                    }
                }
            }
        }


        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int tabIndex = GetTabIndexAt(e.X, e.Y);
                if (tabIndex != -1)
                {
                    ColorDialog colorDialog = new ColorDialog();
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        tabColors[tabIndex] = ColorToTuple(colorDialog.Color);
                        tabControl1.Invalidate();
                    }
                }
            }
        }

        private int GetTabIndexAt(int x, int y)
        {
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(x, y))
                {
                    return i;
                }
            }
            return -1;
        }


        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            TabPage tp = tabControl1.TabPages[e.Index];

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;

            // This is the rectangle to draw "over" the tabpage title
            RectangleF headerRect = new RectangleF(e.Bounds.X, e.Bounds.Y + 2, e.Bounds.Width, e.Bounds.Height - 2);

            // This is the default color to use for the non-selected tabs
            SolidBrush sb = new SolidBrush(Color.AntiqueWhite);

            // Check if this is the second tab (index 1) and change its color
            for (int k0 = 0; k0 < tabColors.Count; k0++)
            {
                if (e.Index == k0)
                    sb.Color = TupleToColor(tabColors[k0]);
            }

            // This changes the color if we're trying to draw the selected tabpage
            if (tabControl1.SelectedIndex == e.Index)
                sb.Color = TupleToColor(tabColors[e.Index]);

            // Check the background color's brightness
            double brightness = (sb.Color.R * 299 + sb.Color.G * 587 + sb.Color.B * 114) / 1000;

            // Set the text color based on the background brightness
            Color textColor = (brightness < 128) ? Color.White : Color.Black;

            // Colour the header of the current tabpage based on what we did above
            g.FillRectangle(sb, e.Bounds);

            // Remember to redraw the text - I'm using the calculated textColor for title text
            g.DrawString(tp.Text, tabControl1.Font, new SolidBrush(textColor), headerRect, sf);
        }


        private void addTabButton_Click(object sender, EventArgs e)
        {
            AddTab();
        }

        private void addressBar_Enter(object sender, EventArgs e)
        {
            addressBar.Select(0, addressBar.Text.Length);
        }
        private void addressBar_Click(object sender, EventArgs e)
        {
            addressBar.SelectAll();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void pathDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Alt+D が押された場合のカスタムアクションを実行
            addressBar.Select();
            // ここにアドレスバーにフォーカスを設定するコードを記述
            addressBar.Focus();
        }


        private void AddTab()
        {
            // 新しいタブを作成
            TabPage newTab = new TabPage("New Tab");
            newTab.Padding = new Padding(100, 100, 100, 100);
            // タブコントロールに追加
            tabControl1.TabPages.Add(newTab);

            // デフォルトのパスを設定して表示
            string defaultPath = @"C:\Users\81905\tanakas\kyousin";
            addressBar.Text = defaultPath;
            ShowFileList(defaultPath);

            // タブと対応するパスを辞書に追加
            tabPaths.Add(newTab.TabIndex, defaultPath);
            tabColors.Add(newTab.TabIndex, ColorToTuple(Color.LightCyan));

        }

        private void AddSelectedPath(string selectedPath)
        {
            // 新しいタブを作成
            string addedPathname = Path.GetFileName(selectedPath);

            TabPage newTab0 = new TabPage(addedPathname);
            newTab0.Padding = new Padding(100, 100, 100, 100);
            // タブコントロールに追加
            tabControl1.TabPages.Add(newTab0);

            addressBar.Text = selectedPath;
            ShowFileList(selectedPath);

            // タブと対応するパスを辞書に追加
            tabPaths.Add(newTab0.TabIndex, selectedPath);

        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 選択されたタブのIndexを取得
            int selectedIndex = tabControl1.SelectedIndex;

            // タブのIndexに対応するパスが辞書に存在する場合、アドレスバーに表示する
            if (tabPaths.ContainsKey(selectedIndex))
            {
                addressBar.Text = tabPaths[selectedIndex];
                ShowFileList(tabPaths[selectedIndex]);
            }
            else
            {
                addressBar.Text = string.Empty;
            }
        }

        private void addressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string newPath = addressBar.Text;

                // 現在選択されているタブのIndexを取得
                int selectedIndex = tabControl1.SelectedIndex;

                // 辞書を更新
                if (tabPaths.ContainsKey(selectedIndex))
                {
                    tabPaths[selectedIndex] = newPath;
                }
                else
                {
                    tabPaths.Add(selectedIndex, newPath);
                }

                if (File.Exists(newPath))
                {
                    // ファイルが存在する場合、ファイルリストを表示する処理を実行する
                    // 例えば、ListViewコントロールを使用してファイルリストを表示するなど
                }
                else if (Directory.Exists(newPath))
                {
                    // ディレクトリが存在する場合、ディレクトリ内のファイルリストを表示する処理を実行する
                    ShowFileList(newPath);
                    UpdateTabName();
                }
            }
        }

        private void UpdateTabName()
        {
            TabPage selectedTab = tabControl1.SelectedTab;

            // 現在のファイルパスを取得
            string filePath = addressBar.Text;

            // ファイルパスからファイル名を抽出
            string fileName = Path.GetFileName(filePath);

            // タブの名前をファイル名に変更
            selectedTab.Text = fileName;
        }

        private void fileListView_DoubleClick(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = fileListView.SelectedItems[0];
                string itemName = selectedItem.Text;
                string itemPath = Path.Combine(addressBar.Text, itemName);

                if (Directory.Exists(itemPath))
                {
                    // フォルダが選択された場合は、そのフォルダ内のファイルリストを表示する
                    ShowFileList(itemPath);
                    addressBar.Text = itemPath; // アドレスバーに新しいパスを表示
                    tabPaths[tabControl1.SelectedIndex] = itemPath;
                    UpdateTabName();
                }
                else if (File.Exists(itemPath))
                {
                    // ファイルが選択された場合は、ファイルを起動する
                    try
                    {
                        System.Diagnostics.Process.Start(itemPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイルの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // ファイルリストが表示されていない領域をダブルクリックした場合は、一つ上のディレクトリに戻る
                string currentPath = addressBar.Text;
                string parentPath = Directory.GetParent(currentPath)?.FullName;

                if (parentPath != null)
                {
                    ShowFileList(parentPath);
                    addressBar.Text = parentPath; // アドレスバーに新しいパスを表示

                }
            }
        }

        private void fileListView_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                try
                {
                    ListViewItem selectedItem = fileListView.SelectedItems[0];
                    string itemName = selectedItem.Text;
                    string itemPath = Path.Combine(addressBar.Text, itemName);

                    if (Directory.Exists(itemPath))
                    {
                        // フォルダがダブルクリックされた場合は、そのフォルダ内のファイルリストを表示する
                        ShowFileList(itemPath);
                        addressBar.Text = itemPath; // アドレスバーに新しいパスを表示
                        tabPaths[tabControl1.SelectedIndex] = itemPath;
                        UpdateTabName();
                    }
                    else if (File.Exists(itemPath))
                    {
                        // ファイルがダブルクリックされた場合は、ファイルを起動する
                        try
                        {
                            System.Diagnostics.Process.Start(itemPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"ファイルの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch(Exception ex0)
                {
                    // ファイルリストが表示されていない領域をダブルクリックした場合は、一つ上のディレクトリに戻る
                    string currentPath = addressBar.Text;
                    string parentPath = Directory.GetParent(currentPath)?.FullName;

                    if (parentPath != null)
                    {
                        ShowFileList(parentPath);                        

                        addressBar.Text = parentPath; // アドレスバーに新しいパスを表示

                        tabPaths[tabControl1.SelectedIndex] = parentPath;

                        UpdateTabName();
                    }
                }


            }
            else if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                // ドラッグ&ドロップの処理


                ListViewHitTestInfo hitTestInfo = fileListView.HitTest(e.X, e.Y);
                if (hitTestInfo.Item != null)
                {
                    List<string> filePaths = new List<string>();


                    List<ListViewItem> selectedItems = new List<ListViewItem>();

                    foreach (ListViewItem selectedItem in fileListView.SelectedItems)
                    {
                        selectedItems.Add(selectedItem);
                        string filePath = Path.Combine(addressBar.Text, selectedItem.Text);
                        filePaths.Add(filePath);
                    }

                    if (filePaths.Count != 0)
                    {
                        string a = filePaths[0];
                        string b = addressBar.Text +"\\" +hitTestInfo.Item.Text;
                        if (filePaths[0] == addressBar.Text + "\\" + hitTestInfo.Item.Text)
                        {
                            // ドラッグ&ドロップ時にデータを保持するオブジェクトを作成
                            DataObject data = new DataObject(DataFormats.FileDrop, filePaths.ToArray());
                            //data.SetData(typeof(List<ListViewItem>), selectedItems);

                            // ドラッグ&ドロップの開始
                            isDragging = true;
                            fileListView.DoDragDrop(data, DragDropEffects.Move);
                            isDragging = false;
                        }
                    }


                }
            }
        }

        private void fileListView_DragEnter(object sender, DragEventArgs e)
        {
            // ドラッグエンター時の処理

            /// アプリ内で完結
            if (e.Data.GetDataPresent(typeof(List<ListViewItem>)))
            {
                e.Effect = DragDropEffects.All;
            }
            /// 外部から来る場合
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }


        private void fileListView_DragDrop(object sender, DragEventArgs e)
        {
            
            /// 外部から来る場合
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (isDragging)
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                    Point dropPoint = fileListView.PointToClient(new Point(e.X, e.Y));
                    ListViewHitTestInfo hitTestInfo = fileListView.HitTest(dropPoint.X, dropPoint.Y);


                    for (int i = 0; i < files.Length; i++)
                    {
                        string fileName = files[i];

                        string sourcePath = fileName;
                        string targetPath;
                        if (hitTestInfo.Item != null)
                        {
                            targetPath = Path.Combine(addressBar.Text, hitTestInfo.Item.Text, Path.GetFileName(fileName));
                        }
                        else
                        {
                            targetPath = Path.Combine(addressBar.Text, Path.GetFileName(fileName));
                        }

                        try
                        {
                            File.Move(sourcePath, targetPath);
                            ShowFileList(addressBar.Text);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                // フォルダを移動
                                string folderName = new DirectoryInfo(sourcePath).Name;
                                if (hitTestInfo.Item != null)
                                {
                                    targetPath = Path.Combine(addressBar.Text, hitTestInfo.Item.Text, Path.GetFileName(fileName));
                                }
                                else
                                {
                                    targetPath = Path.Combine(addressBar.Text, Path.GetFileName(fileName));
                                }
                                targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileName(fileName));

                                Directory.Move(sourcePath, targetPath);
                                ShowFileList(addressBar.Text);
                            }
                            catch (Exception ex0)
                            {
                                //MessageBox.Show($"ファイルの移動に失敗しました: {ex0.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }

            }

            /// アプリ内で完結
            else if (e.Data.GetDataPresent(typeof(List<ListViewItem>)))
            {
                if (isDragging)
                {
                    Point dropPoint = fileListView.PointToClient(new Point(e.X, e.Y));
                    ListViewHitTestInfo hitTestInfo = fileListView.HitTest(dropPoint.X, dropPoint.Y);
                    List<ListViewItem> selectedItems = (List<ListViewItem>)e.Data.GetData(typeof(List<ListViewItem>));

                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        if (hitTestInfo.Item != null && hitTestInfo.Item != selectedItem)
                        {
                            string sourcePath = Path.Combine(addressBar.Text, selectedItem.Text);
                            string targetPath = Path.Combine(addressBar.Text, hitTestInfo.Item.Text);
                            targetPath = Path.Combine(IsDirectory(targetPath) ? targetPath : Path.GetDirectoryName(targetPath), selectedItem.Text);


                            if (sourcePath != targetPath)
                            {
                                try
                                {
                                    File.Move(sourcePath, targetPath);
                                    ShowFileList(addressBar.Text);
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        // フォルダを移動
                                        string folderName = new DirectoryInfo(sourcePath).Name;
                                        targetPath = Path.Combine(addressBar.Text, hitTestInfo.Item.Text);
                                        targetPath = Path.Combine(IsDirectory(targetPath) ? targetPath : Path.GetDirectoryName(targetPath), selectedItem.Text);

                                        if (sourcePath != targetPath)
                                        {
                                            Directory.Move(sourcePath, targetPath);
                                            ShowFileList(addressBar.Text);
                                        }
                                    }
                                    catch (Exception ex0)
                                    {
                                        MessageBox.Show($"ファイルの移動に失敗しました: {ex0.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }
                        }
                    }

                }



            }
        }

        private bool IsDirectory(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.Directory);
        }

        private void ShowFileList(string path)
        {
            // ファイルリストをクリア
            fileListView.Items.Clear();

            // ディレクトリ内のファイルとフォルダを取得
            string[] files = Directory.GetFiles(path);
            string[] folders = Directory.GetDirectories(path);

            // アイコンを表示するための ImageList を作成
            fileListView.SmallImageList = new ImageList();
            fileListView.SmallImageList.ImageSize = new Size(20, 20);


            // フォルダをListViewに追加
            foreach (string folderPath in folders)
            {
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                ListViewItem item = new ListViewItem(folder.Name);

                // フォルダのアイコンを取得
                string iconPath = @"./Folder-transformed.ico";
                // パスを指定してアイコンのインスタンスを生成
                Icon folderIcon = new Icon(iconPath, 16, 16);

                //Icon folderIcon = GetFolderIcon();
                fileListView.SmallImageList.Images.Add(folderIcon);
                item.ImageIndex = fileListView.SmallImageList.Images.Count - 1;

                item.SubItems.Add(""); // サイズのカラム用の空のサブアイテムを追加
                item.SubItems.Add("Folder"); // サイズのカラム用の空のサブアイテムを追加
                item.SubItems.Add(folder.CreationTime.ToString()); // 作成日時を表示
                fileListView.Items.Add(item);
            }

            // ファイルをListViewに追加
            foreach (string filePath in files)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                ListViewItem item = new ListViewItem(fileInfo.Name);

                // ファイルのアイコンを取得
                Icon fileIcon = GetFileIcon(filePath);
                fileListView.SmallImageList.Images.Add(fileIcon);
                //fileListView.SmallImageList.Images.Add(fileIcon.ToBitmap());
                item.ImageIndex = fileListView.SmallImageList.Images.Count - 1;

                item.SubItems.Add(GetFileSizeString(fileInfo.Length)); // ファイルサイズを表示
                item.SubItems.Add(GetFileType(filePath)); // サイズのカラム用の空のサブアイテムを追加

                item.SubItems.Add(fileInfo.CreationTime.ToString()); // 作成日時を表示
                fileListView.Items.Add(item);
            }

            this.Text = Path.GetFileName(path) + " - Explorer X";
        }


        private void tabControl1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths != null && filePaths.Length > 0)
                {
                    e.Effect = DragDropEffects.Move;
                }
            }
        }

        private void tabControl1_DragDrop(object sender, DragEventArgs e)
        {
            if (isDrag)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    TabControl tabControl = (TabControl)sender;
                    Point dropPoint = tabControl.PointToClient(new Point(e.X, e.Y));
                    int targetTabIndex = -1;

                    for (int i = 0; i < tabControl.TabCount; i++)
                    {
                        Rectangle tabRect = tabControl.GetTabRect(i);
                        if (tabRect.Contains(dropPoint))
                        {
                            targetTabIndex = i;
                            break;
                        }
                    }

                    if (targetTabIndex != -1)
                    {
                        // 移動先のタブが見つかった場合、ファイルを移動する処理を実行
                        for (int j = 0; j < files.Length; j++)
                        {
                            string targetTabText = tabControl.TabPages[targetTabIndex].Text;
                            string targetPath = Path.Combine(tabPaths[targetTabIndex], Path.GetFileName(files[0]));

                            try
                            {
                                File.Move(files[j], targetPath);
                                ShowFileList(tabPaths[targetTabIndex]);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    Directory.Move(files[j], targetPath);
                                    ShowFileList(tabPaths[targetTabIndex]);
                                }
                                catch(Exception ex0)
                                {
                                    MessageBox.Show($"ファイルの移動に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                            }
                        }


                    }
                }
            }
            

        }

        private Icon GetFolderIcon()
        {
            // システムのデフォルトのフォルダアイコンを取得
            return Icon.ExtractAssociatedIcon(Environment.SystemDirectory + @"\shell32.dll");
        }

        private Icon GetFileIcon(string filePath)
        {
            // ファイルの拡張子から関連付けられたアイコンを取得
            return Icon.ExtractAssociatedIcon(filePath);
        }

        private string GetFileSizeString(long fileSize)
        {
            const int kb = 1024;
            const string sizeUnit = "KB";

            long size = fileSize / kb;
            return $"{size} {sizeUnit}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(lockBottun.Text == "Lock")
            {
                this.TopMost = true;
                lockBottun.Text = "Locked";
                lockBottun.BackColor = ColorTranslator.FromHtml("#e88eb3");
            }
            else if(lockBottun.Text == "Locked")
            {
                this.TopMost = false;
                lockBottun.Text = "Lock";
                lockBottun.BackColor = ColorTranslator.FromHtml("#8ee4e8");
            }

        }

        private void fileListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // 元の描画処理を実行
            e.DrawDefault = true;


            // ヘッダの背景色を設定
            using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(25, 25, 25))) // #191919
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            // ヘッダのテキストを描画
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds);
            }

            // ヘッダ項目の区切り線を描画
            using (Pen separatorPen = new Pen(Color.White))
            {
                e.Graphics.DrawLine(separatorPen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            }

            e.DrawText(); // ヘッダのテキストを描画
        }

        private void fileListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (e.ItemIndex >= 0)
            {
                // ファイルの内容を描画
                e.DrawDefault = true;
            }


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                int selectedIndex = tabControl1.SelectedIndex;
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                if (tabPaths.ContainsKey(selectedIndex))
                {
                    tabPaths.Remove(selectedIndex);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            y0 -= 20;
            h += 20;
            
            fileListView.Location = new System.Drawing.Point(x0, y0);
            fileListView.Size = new System.Drawing.Size(w, h);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            y0 += 20;
            h -= 20;

            fileListView.Location = new System.Drawing.Point(x0, y0);
            fileListView.Size = new System.Drawing.Size(w, h);
        }

        private void アプリケーションの終了XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openInExplorerEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //エクスプローラでフォルダ"C:\My Documents\My Pictures"を開く
            System.Diagnostics.Process.Start(
                "EXPLORER.EXE", tabPaths[tabControl1.SelectedIndex]);
        }

        private void fileListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                ShowFileList(addressBar.Text);
            }

            if (e.KeyCode == Keys.F2)
            {
                // F2キーが押された場合、選択されたファイルの名称を編集モードに切り替える
                fileListView.SelectedItems[0].BeginEdit();
            }

            if (e.Control && e.KeyCode == Keys.C)
            {
                e.Handled = true;

                // 選択されたファイルのパスをクリップボードにコピー
                if (fileListView.SelectedItems.Count > 0)
                {
                    List<string> selectedFilePaths = new List<string>();
                    foreach (ListViewItem selectedItem in fileListView.SelectedItems)
                    {
                        string filePath = Path.Combine(addressBar.Text, selectedItem.Text);
                        selectedFilePaths.Add(filePath);
                    }
                    string fileList = string.Join(Environment.NewLine, selectedFilePaths);
                    Clipboard.SetText(fileList);
                }
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                // Ctrl+Shift+Cが押された場合、選択されたファイルのパスをクリップボードにコピーする
                CopySelectedFilePathToClipboard();
            }

            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;

                // クリップボードからファイルパスを取得
                string clipboardText = Clipboard.GetText();
                string[] filePaths = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                // ファイルを目的の場所にコピー
                foreach (string filePath in filePaths)
                {
                    string fileName = Path.GetFileName(filePath);
                    string targetPath = Path.Combine(addressBar.Text, fileName);

                    try
                    {
                        File.Copy(filePath, targetPath);
                        ShowFileList(addressBar.Text);
                        
                        // 必要に応じてファイルリストを更新するなどの処理を追加
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイルのコピーに失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            // Deleteキーが押されたとき
            if (e.KeyCode == Keys.Delete)
            {
                // 選択されたアイテムをゴミ箱に移動（SendToRecycleBinメソッドを定義してください）
                foreach (ListViewItem selectedItem in fileListView.SelectedItems)
                {
                    string filePath = Path.Combine(addressBar.Text, selectedItem.Text);
                    SendToRecycleBin(filePath);
                }
                // FileListViewの表示を更新
                string currentPath = addressBar.Text;
                ShowFileList(currentPath);
            }
            // Shift+Deleteキーが押されたとき
            else if (e.Modifiers == Keys.Shift && e.KeyCode == Keys.Delete)
            {
                // 選択されたアイテムを完全に削除
                foreach (ListViewItem selectedItem in fileListView.SelectedItems)
                {
                    string filePath = Path.Combine(addressBar.Text, selectedItem.Text);
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        else if (Directory.Exists(filePath))
                            Directory.Delete(filePath, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"削除に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                // FileListViewの表示を更新
                string currentPath = addressBar.Text;
                ShowFileList(currentPath);
            }
        }

        private void CopySelectedFilePathToClipboard()
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                string filePath = fileListView.SelectedItems[0].ToolTipText;
                if (filePath != null)
                {
                    try
                    {
                        Clipboard.SetText(filePath);
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
        }


        private void fileListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            isEditing = false;
            if (e.Label != null)
            {
                // ファイル名が変更された場合、新しいファイル名を取得して処理を行う
                string newFileName = e.Label;
                string filePath =  addressBar.Text +"/"+ fileListView.SelectedItems[0].Text;

                if (File.Exists(filePath))
                {
                    // ファイル名を変更する
                    try
                    {
                        string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
                        File.Move(filePath, newFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイル名の変更に失敗しました。\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.CancelEdit = true; // 変更をキャンセルする
                    }
                }
                else if (Directory.Exists(filePath))
                {
                    // ディレクトリ名を変更する
                    try
                    {
                        string newDirPath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
                        Directory.Move(filePath, newDirPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ディレクトリ名の変更に失敗しました。\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.CancelEdit = true; // 変更をキャンセルする
                    }
                }
            }
        }

        private void fileListView_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            // F2キーでのファイル名変更を許可する
            e.CancelEdit = !isEditing;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // F2キーを押すとファイル名を編集モードにする
            if (keyData == Keys.F2)
            {
                if (fileListView.SelectedIndices.Count == 1)
                {
                    fileListView.LabelEdit = true;
                    originalFileName = fileListView.SelectedItems[0].Text; // 編集前のファイル名を保持する
                    pathBeforeChanged = fileListView.SelectedItems[0].ToolTipText;
                    fileListView.SelectedItems[0].BeginEdit();
                    isEditing = true;
                    return true;
                }
            }
            // Enterキーでファイル名の編集を確定する
            else if (keyData == Keys.Enter)
            {
                if (fileListView.SelectedIndices.Count == 1 && fileListView.LabelEdit)
                {
                    fileListView.LabelEdit = false;
                    isEditing = false;
                    return true;
                }
            }
            // Escキーでファイル名の編集をキャンセルする
            else if (keyData == Keys.Escape)
            {
                if (fileListView.SelectedIndices.Count == 1 && fileListView.LabelEdit)
                {
                    fileListView.LabelEdit = false;
                    fileListView.SelectedItems[0].Text = originalFileName; // 編集前のファイル名に戻す
                    isEditing = false;
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        // ゴミ箱にファイルを移動するメソッド
        private void SendToRecycleBin(string filePath)
        {
            FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // フォームを閉じる前に開かれたタブの情報を保存する
            using (StreamWriter writer = new StreamWriter("tabs.txt"))
            {
                foreach (KeyValuePair<int, string> tabInfo in tabPaths)
                {
                    writer.WriteLine($"{tabInfo.Key}\t{tabInfo.Value}");
                }
            }

            SaveTabColorsToFile();
        }


        private string GetFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();

            // ファイルの拡張子に関連付けられたファイルタイプを取得
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension);
            if (key != null)
            {
                object fileTypeValue = key.GetValue(null);
                key.Close();

                if (fileTypeValue != null)
                {
                    string fileType = fileTypeValue.ToString();
                    // ファイルタイプのラベルを取得
                    key = Registry.ClassesRoot.OpenSubKey(fileType);
                    if (key != null)
                    {
                        object fileLabelValue = key.GetValue(null);
                        key.Close();
                        if (fileLabelValue != null)
                        {
                            return fileLabelValue.ToString();
                        }
                    }
                }
            }

            // ファイルタイプが特定できない場合はデフォルトのラベルを返す
            return "File";
        }


    }

}
