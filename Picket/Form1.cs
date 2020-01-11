using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Picket
{
    public partial class Form1 : Form
    {
        readonly object _lock1 = new object();
        readonly object _lock2 = new object();
        private bool StopCalledFlag { get; set; } = false;
        private Thread ActionThread { get; set; }
        private Timer ReverseListTimer { get; set; }
        private PowerType PT { get; set; } = PowerType.Random;
        private MyReflection.ReflectionData Instance { get; set; }
        private List<AssemblyName> AssList { get; } = new List<AssemblyName>();

        public Form1()
        {
            InitializeComponent();
            ListViewSetGUI();
            AdvancedToolStripMenuItem_Click(null, null);
            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\plugins\");
            di.GetFiles("*.dll");
            foreach (FileInfo f in di.GetFiles("*.dll"))
            {
                AssList.Add(AssemblyName.GetAssemblyName(f.FullName));
                ToolStripMenuItem item = new ToolStripMenuItem(AssList.Last().Name, null, LoadAssembly, AssList.Last().Name);
                pluginToolStripMenuItem.DropDownItems.Add(item);
            }
            TrackBar1_Scroll(null, null);
            button4.BackColor = ColorRandom();
            ButtonStates(false);
            label3.Parent = pluginBG;
            label3.Update();
            label8.Parent = pluginBG;
            label8.Update();
            UpdateEntryCount();
        }

        #region Assembly
        private void ClearAssembly()
        {
            if (Instance != null)
            {
                bool check1 = Convert.ToBoolean(Instance.Properties.IsBusy.GetValue(Instance.iRandomInstance));
                if (check1)
                {
                    Instance.Methods.Stop.Invoke(Instance.iRandomInstance, null);
                }

                bool check2 = Convert.ToBoolean(Instance.Properties.IsDisposed.GetValue(Instance.iRandomInstance));
                if (!check2)
                {

                    Instance.Methods.Dispose.Invoke(Instance.iRandomInstance, null);
                }

                while (check1 || !check2)
                {
                    check1 = Convert.ToBoolean(Instance.Properties.IsBusy.GetValue(Instance.iRandomInstance));
                    check2 = Convert.ToBoolean(Instance.Properties.IsDisposed.GetValue(Instance.iRandomInstance));
                    Thread.Sleep(10);
                }
                for (int i = 0; i < Instance.ActionEvents.Count; i++)
                {
                    Instance.ActionEvents[i].RemoveEventHandler(Instance.iRandomInstance, Instance.ActionDelegates[i]);
                }
                Instance.Methods.EntriesClear.Invoke(Instance.iRandomInstance, null);
                Instance = null;
                label3.Visible = true;
                label8.Visible = true;
                label1.Text = "";
                label1.BackColor = Color.Snow;
                progressBar1.Value = 0;
                ButtonStates(false);
            }
        }// Clear assembly settings and dispose of plugin
        private void LoadAssembly(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.Load(AssList.FirstOrDefault(x => x.Name == ((ToolStripMenuItem)sender).Name));

            ClearAssembly();
            label3.Visible = false;
            label8.Visible = false;
            ButtonStates(true);

            Instance = new MyReflection(pluginBG, assembly).Instance;

            Instance.ToolProperties.ForceUniqueEntryColors.SetValue(Instance.ToolPropertiesObj, true);
            Instance.ToolProperties.ArrowPosition.SetValue(Instance.ToolPropertiesObj, (int)ArrowLocation.Left);
            Instance.ToolProperties.TextToShow.SetValue(Instance.ToolPropertiesObj, (int)TextType.Name);
            Instance.Properties.AllowExceptions.SetValue(Instance.iRandomInstance, false);

            UpdateEntryList();

            Instance.Methods.Draw.Invoke(Instance.iRandomInstance, new object[] { 13, 4, 150 });
        }// Load Assembly and initial settings.
        private void ButtonStates(bool state = true)
        {
            button1.Enabled = state;
            button2.Enabled = state;
            if (!state)
            {
                button5.Enabled = state;
                button6.Enabled = state;
            }//do not default enable
            button7.Enabled = state;
        }
        #endregion

        #region Events
        private void TryContinueSpin()
        {
            if (checkBox1.Checked && (radioButton1.Checked || (radioButton2.Checked && numericUpDown2.Value > 0)))
            {
                if (radioButton2.Checked && numericUpDown2.Value > 0)
                { numericUpDown2.Value--; }

                if (radioButton2.Checked && numericUpDown2.Value == 0)
                { return; }

                if (listView1.Items.Count > 0)
                {
                    Button1_Click(null, null);
                }

            }
        }
        private void EventStopCall(object entry)
        {
            Invoke((MethodInvoker)delegate
            { DoEventAction(entry, null, 100); });

            // Create delay for effect
            // This allows the above Invoked to finish before moving the ticket.
            Timer StopEntryTimer = new Timer();
            StopEntryTimer.Elapsed += (sender, e) => EventStopCallDelay(sender, e, entry);
            StopEntryTimer.AutoReset = false;
            StopEntryTimer.Interval = 1000;
            StopEntryTimer.Start();
        }
        private void EventStopCallDelay(object sender, ElapsedEventArgs e, object entry)
        {
            Invoke((MethodInvoker)delegate
            {
                if (PT == PowerType.Infinite || !StopCalledFlag)
                {
                    MoveEntryEvent(entry);
                    TryContinueSpin();
                    UpdateEntryCount();
                }
            });
            StopCalledFlag = false;
        }
        private void EventActionCall(object entry, string[] actionInfo)
        {
            if (!StopCalledFlag)
            {
                Invoke((MethodInvoker)delegate
                {
                    DoEventAction(entry, actionInfo);
                });
            }
        }
        private void DoEventAction(object entry, string[] actionInfo, int progressBar = -1)
        {
            ReverseListUpdateTimer_Set();

            lock (_lock2)
            {
                label1.Text = $"{ GetPropValue(entry, "Name") }";
                label1.BackColor = (Color)GetPropValue(entry, "Aura");
                label1.Refresh();
                ProgressUpdate(actionInfo, progressBar);
            }
        }
        public void MoveEntryEvent(object entry)
        {
            string name = GetPropValue(entry, "Name").ToString();
            int tkt = (int)GetPropValue(entry, "UniqueID");

            ListViewItem pickedItem = ListViewTryFindText(listView1, name);
            int countLV2 = listView2.Items.Count + 1;
            int countLV3 = listView3.Items.Count + 1;

            // Remove items from Plugin List first and listView1 second.
            // This process will keep the order of the Plugin List.
            // Useful if someone has used Shuffle.

            if (moveWinningTicketToolStripMenuItem.Checked)
            {
                Instance.Methods.EntryRemove.Invoke(Instance.iRandomInstance, new object[] { tkt });
                ReverseUpdateEntryList();
                listView2.Items.Add(new ListViewItem(new string[] { name, countLV2.ToString() }, 0, pickedItem.ForeColor, pickedItem.BackColor, default));
            }
            else
            {
                listView3.Items.Add(new ListViewItem(new string[] { name, countLV3.ToString() }, 0, pickedItem.ForeColor, pickedItem.BackColor, default));
            }
            if (removeWinnerFromPoolToolStripMenuItem.Checked)
            {
                var entryList = CallEntryList();
                List<int> tickets = new List<int>();
                foreach (dynamic item in entryList)
                {
                    if (item.Name == GetPropValue(entry, "Name").ToString())
                    { tickets.Add((int)item.UniqueID); }
                }
                foreach (int item in tickets)
                {
                    Instance.Methods.EntryRemove.Invoke(Instance.iRandomInstance, new object[] { item });
                }
                ReverseUpdateEntryList();
            }
            Instance.Methods.Refresh.Invoke(Instance.iRandomInstance, new object[] { });
            
        }// Finishes the round - Moves tickets if needed
        #endregion

        #region ListUpdate
        private void ListView1_Update_Ticket(TicketUpdateType ticketUpdateType = TicketUpdateType.Add, string name = null, int newcnt = -1, Color? backColor = null, Color? foreColor = null)
        {
            backColor = backColor ?? button4.BackColor;
            foreColor = foreColor ?? Color.Black;
            name = name ?? textBox1.Text.Trim();
            if (newcnt == -1) { newcnt = (int)numericUpDown1.Value; }

            // Prioritize functions from Plugin before built in function.
            // Plugins may be programmed differently.
            if (Instance != null)
            {
                bool check = Convert.ToBoolean(Instance.Methods.IsReadable.Invoke(Instance.iRandomInstance, new object[] { (Color)backColor, (Color)foreColor }));
                if (!check) { foreColor = Color.White; }
            }
            else
            {
                if (!IsReadable((Color)backColor, (Color)foreColor)) { foreColor = Color.White; }
            }

            ListViewItem item = ListViewTryFindText(listView1, name);
            if (item != null)
            {
                int cnt;
                int loc = listView1.Items.IndexOf(item);
                int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                if (ticketUpdateType == TicketUpdateType.Add) { cnt += newcnt; }
                else if (ticketUpdateType == TicketUpdateType.Subtract) { cnt -= newcnt; }
                else if (ticketUpdateType == TicketUpdateType.Update) { cnt = newcnt; }

                if (cnt <= 0)
                { listView1.Items[loc].Remove(); }
                else
                {
                    listView1.Items[loc].SubItems[1].Text = cnt.ToString();
                    listView1.Items[loc].BackColor = (Color)backColor;
                    listView1.Items[loc].ForeColor = (Color)foreColor;
                }
            }
            else
            {
                listView1.Items.Add(new ListViewItem(new string[] { name, newcnt.ToString() }, 0, (Color)foreColor, (Color)backColor, default));
            }
            TextBox1_TextChanged(null, null);
            UpdateEntryList();
        }
        private ListViewItem ListViewTryFindText(ListView listView, string text)
        {
            ListViewItem item = null;
            if (listView.Items.Count > 0)
            {
                item = listView.FindItemWithText(text, true, 0, false);
            }
            return item;
        }
        private void ReverseListUpdateTimer_Set()
        {
            if (ReverseListTimer == null)
            {
                ReverseListTimer = new System.Timers.Timer();
                ReverseListTimer.Elapsed += new ElapsedEventHandler(ReverseListUpdateTimer_Elapsed);
                ReverseListTimer.AutoReset = false;
                ReverseListTimer.Interval = 200;
                ReverseListTimer.Start();
            }
        }
        private void ReverseListUpdateTimer_Clear()
        {
            if (ReverseListTimer != null)
            {
                ReverseListTimer.Stop();
                ReverseListTimer.Dispose();
                ReverseListTimer = null;
            }
        }
        private void ReverseListUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock1)
            {
                try
                {
                    if (listView1.InvokeRequired)
                    {
                        listView1.Invoke((MethodInvoker)delegate
                        {
                            ReverseUpdateEntryList();
                        });
                    }
                    else
                    {
                        ReverseUpdateEntryList();
                    }
                }
                catch
                { }
            
                ReverseListUpdateTimer_Clear();
            }
        }
        private void ReverseUpdateEntryList()
        {
            var entryList = CallEntryList();
            if (entryList == null) { return; }
            listView1.Items.Clear();

            object[] readColors = new object[2] { null, null };
            string[] listItem = new string[2] { null, null };

            foreach (dynamic entry in entryList)
            {
                Color c1 = entry.Aura;
                Color c2 = Color.Black;
                readColors[0] = c1; readColors[1] = c2;
                bool check = Convert.ToBoolean(Instance.Methods.IsReadable.Invoke(Instance.iRandomInstance, readColors));
                if (!check) { c2 = Color.White; }

                string name = entry.Name;
                int cnt = 1;

                ListViewItem item = ListViewTryFindText(listView1, name);
                if (item != null)
                {
                    int loc = listView1.Items.IndexOf(item);
                    int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                    cnt++;

                    listView1.Items[loc].SubItems[1].Text = cnt.ToString();
                    listView1.Items[loc].BackColor = c1;
                    listView1.Items[loc].ForeColor = c2;
                }
                else
                {
                    listItem[0] = name; listItem[1] = cnt.ToString();
                    listView1.Items.Add(new ListViewItem(listItem, 0, c2, c1, default));
                    TextBox1_TextChanged(null, null);
                }
            }
        }
        private dynamic CallEntryList()
        {
            if (Instance == null) { return null; }
            dynamic list = Instance.EntryList;
            return list;
        }
        private void UpdateEntryCount()
        {
            int cnt1 = 0;
            foreach (ListViewItem item in listView1.Items)
            { cnt1 += int.Parse(item.SubItems[1].Text); }
            int cnt2 = moveWinningTicketToolStripMenuItem.Checked ? listView2.Items.Count : listView3.Items.Count;

            labelTktListCnt.Text = cnt1.ToString();
            labelWinListCnt.Text = cnt2.ToString();
        }
        private void UpdateEntryList()
        {
            UpdateEntryCount();

            if (Instance != null)
            {
                object[] nullObj = new object[] { };
                object[] entry = new object[1] { null };
                Instance.Methods.EntriesClear.Invoke(Instance.iRandomInstance, nullObj);
                foreach (ListViewItem item in listView1.Items)
                {
                    int cnt = int.Parse(item.SubItems[1].Text);
                    for (int i = 0; i < cnt; i++)
                    {
                        entry[0] = NewEntry(item.Text, item.BackColor);
                        Instance.Methods.EntryAdd.Invoke(Instance.iRandomInstance, entry);
                    }
                }
                Instance.Methods.Refresh.Invoke(Instance.iRandomInstance, nullObj);
            }
        }
        public object NewEntry(string s, Color c, int id = -1)
        {
            object e = Activator.CreateInstance(Instance.EntryType);
            PropertyInfo entryNameP = Instance.EntryType.GetProperty("Name");
            entryNameP.SetValue(e, s);
            PropertyInfo entryColorP = Instance.EntryType.GetProperty("Aura");
            entryColorP.SetValue(e, c);
            PropertyInfo entryIDP = Instance.EntryType.GetProperty("UniqueID");
            entryIDP.SetValue(e, id);
            return e;
        }
        #endregion

        #region Button Clicks
        private void Button1_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            ActionThread = new Thread(() => { try { Instance.Methods.Start.Invoke(Instance.iRandomInstance, new object[] { (int)Direction.Clockwise, (int)PT, 5 }); } catch { } });
            ActionThread.SetApartmentState(ApartmentState.MTA);
            ActionThread.Start();
        }// Start
        private void Button2_Click(object sender, EventArgs e)
        {
            bool check = Convert.ToBoolean(Instance.Properties.IsBusy.GetValue(Instance.iRandomInstance));
            if (check)
            {
                StopCalledFlag = true;

                ReverseListUpdateTimer_Clear();
                if (PT != PowerType.Infinite) { checkBox1.Checked = false; }

                Instance.Methods.Stop.Invoke(Instance.iRandomInstance, null);
            }
        }// Stop
        private void Button3_Click(object sender, EventArgs e)
        {
            ListView1_Update_Ticket(TicketUpdateType.Add);
        }// Add
        private void Button4_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();
            if (result != DialogResult.Cancel)
            {
                button4.BackColor = colorDialog1.Color;
            }
        }// ColorBox
        private void Button5_Click(object sender, EventArgs e)
        {
            ListView1_Update_Ticket(TicketUpdateType.Update);
        }// Update
        private void Button6_Click(object sender, EventArgs e)
        {
            ListView1_Update_Ticket(TicketUpdateType.Subtract);
        }// Subtract
        private void Button7_Click(object sender, EventArgs e)
        {
            Instance.Methods.ShuffleEntries.Invoke(Instance.iRandomInstance, new object[] { });
            ReverseUpdateEntryList();
            Instance.Methods.Refresh.Invoke(Instance.iRandomInstance, new object[] { });
        }// Shuffle
        private void Button8_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                int loc = listView2.SelectedItems[0].Index;
                ListViewItem item = listView2.Items[loc];
                string name = item.Text;
                Color c1 = item.BackColor;
                Color c2 = item.ForeColor;
                item.Remove();

                for (int i = loc; i < listView2.Items.Count; i++)
                {
                    item = listView2.Items[i];
                    int cnt;
                    int.TryParse(item.SubItems[1].Text, out cnt);
                    item.SubItems[1].Text = (cnt - 1).ToString();
                }
                ListView1_Update_Ticket(TicketUpdateType.Add, name, 1, c1, c2);
            }
        }// Move back winner
        private void Button9_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            UpdateEntryCount();
        }// Clear Winners
        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem item = listView1.SelectedItems[0];
            textBox1.Text = item.SubItems[0].Text;
            try { numericUpDown1.Value = int.Parse(item.SubItems[1].Text); } catch { }
            button4.BackColor = item.BackColor;
        }
        private void ListView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewColumnSorter columnSorter = (ListViewColumnSorter)listView1.ListViewItemSorter;
            if (columnSorter == null) { columnSorter = new ListViewColumnSorter(); }

            if (e.Column == columnSorter.SortColumn)
            {
                if (columnSorter.Order == SortOrder.Ascending)
                {
                    columnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    columnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                columnSorter.SortColumn = e.Column;
                columnSorter.Order = SortOrder.Ascending;
            }
            listView1.ListViewItemSorter = columnSorter;
            listView1.Sort();
            UpdateEntryList();
        }
        #endregion

        #region ToolStrip Clicks
        private void AboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
        }
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private void ClearPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAssembly();
            if (ActionThread != null && ActionThread.IsAlive) { ActionThread.Abort(); ActionThread.Join(); }
        }
        private void ClearTicketListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            if (Instance != null)
            {
                Instance.Methods.EntriesClear.Invoke(Instance.iRandomInstance, new object[] { });
                Instance.Methods.Refresh.Invoke(Instance.iRandomInstance, new object[] { });
            }
            UpdateEntryCount();
        }
        private void ClearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearPluginToolStripMenuItem_Click(null, null);
            ClearTicketListToolStripMenuItem_Click(null, null);
            ClearWinnersToolStripMenuItem_Click(null, null);
            textBox1.Text = "";
        }
        private void MoveWinningTicketToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveWinningTicketToolStripMenuItem.Checked = moveWinningTicketToolStripMenuItem.Checked ? false : true;
            if (listView2.Items.Count > 0 || listView3.Items.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show("Switching this setting will cause current winners to be cleared.\r\nContinue?", "Picket Warning", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (listView2.Items.Count > 0)
                    {
                        dialogResult = MessageBox.Show("Move picked tickets back to original list?", "Picket Warning", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            foreach (ListViewItem item in listView2.Items)
                            {
                                item.Selected = true;
                                Button8_Click(null, null);
                            }
                        }
                    }
                    listView3.Items.Clear();
                }
            }
            UpdateEntryCount();
            ListViewSetGUI();
        }
        private void RemoveWinnerFromPoolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            removeWinnerFromPoolToolStripMenuItem.Checked = removeWinnerFromPoolToolStripMenuItem.Checked ? false : true;
            if (removeWinnerFromPoolToolStripMenuItem.Checked)
            {
                DialogResult dialogResult = MessageBox.Show("Using this feature may cause loss of tickets in your pool.\r\nContinue?", "Picket Warning", MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes)
                {
                    removeWinnerFromPoolToolStripMenuItem.Checked = false;
                }
            }
        }
        private void ClearWinnersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            listView3.Items.Clear();
            UpdateEntryCount();
        }
        private void AdvancedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender != null) { advancedToolStripMenuItem.Checked = advancedToolStripMenuItem.Checked ? false : true; }

            if (advancedToolStripMenuItem.Checked)
            {
                panel1.Visible = true;
                button7.Visible = true;
                listView1.Height = 160;
                Size p = new Size(775, Height);
                MinimumSize = p;
                MaximumSize = p;
            }
            else
            {
                panel1.Visible = false;
                button7.Visible = false;
                listView1.Height = 160 + 25;
                Size p = new Size((775 - 105), Height);
                MinimumSize = p;
                MaximumSize = p;
            }
        }
        private void SaveSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            saveFileDialog1.Filter = "Picket File (.pkt)|*.pkt|INI File (.ini)|*.ini|Text File (.txt)|*.txt|All Files (.*)|*.*";
            saveFileDialog1.OverwritePrompt = true;
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.Cancel)
            {
                string write = "";
                FileStream fs = File.Open(saveFileDialog1.FileName, FileMode.Create);

                write += "[listView1]\r\n";
                foreach (ListViewItem item in listView1.Items)
                {
                    int cnt = int.Parse(item.SubItems[1].Text);
                    write += $"Entry={item.Text}|{item.BackColor}|{item.ForeColor}|{cnt}\r\n";
                }
                write += "[listView2]\r\n";
                foreach (ListViewItem item in listView2.Items)
                {
                    int cnt = int.Parse(item.SubItems[1].Text);
                    write += $"Entry={item.Text}|{item.BackColor}|{item.ForeColor}|{cnt}\r\n";
                }
                write += "[listView3]\r\n";
                foreach (ListViewItem item in listView3.Items)
                {
                    int cnt = int.Parse(item.SubItems[1].Text);
                    write += $"Entry={item.Text}|{item.BackColor}|{item.ForeColor}|{cnt}\r\n";
                }
                write += "[Plugin]\r\n";
                write += Instance.Assembly.FullName.Replace(',','|') + "\r\n";

                byte[] bytes = Encoding.ASCII.GetBytes(write);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
        }

        private void RestoreSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog1.Filter = "Picket File (.pkt)|*.pkt|INI File (.ini)|*.ini|Text File (.txt)|*.txt|All Files (.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result != DialogResult.Cancel)
            {
                ClearAllToolStripMenuItem_Click(null, null);
                string text = File.ReadAllText(openFileDialog1.FileName);
                text = text.Replace("\r\n", "`");
                List<string> textList = text.Split('`').ToList<string>();
                int loc1 = textList.FindIndex(x => x == "[listView1]");
                int loc2 = textList.FindIndex(x => x == "[listView2]");
                int loc3 = textList.FindIndex(x => x == "[listView3]");
                int loc4 = textList.FindIndex(x => x == "[Plugin]");

                for (int i = loc1 + 1; i < loc2; i++)
                {
                    string[] textArr = textList[i].Replace("Entry=", "").Replace("Color ", "").Replace("[", "").Replace("]", "").Split('|');
                    ListView1_Update_Ticket(TicketUpdateType.Update, textArr[0], int.Parse(textArr[3]), Color.FromName(textArr[1]), Color.FromName(textArr[2]));
                }
                for (int i = loc2 + 1; i < loc3; i++)
                {
                    string[] textArr = textList[i].Replace("Entry=", "").Replace("Color ", "").Replace("[", "").Replace("]", "").Split('|');
                    listView2.Items.Add(new ListViewItem(new string[] { textArr[0], textArr[3] }, 0, Color.FromName(textArr[2]), Color.FromName(textArr[1]), default));
                }
                for (int i = loc3 + 1; i < loc4; i++)
                {
                    string[] textArr = textList[i].Replace("Entry=", "").Replace("Color ", "").Replace("[", "").Replace("]", "").Split('|');
                    listView3.Items.Add(new ListViewItem(new string[] { textArr[0], textArr[3] }, 0, Color.FromName(textArr[2]), Color.FromName(textArr[1]), default));
                }
                ToolStripItem tsi = pluginToolStripMenuItem.DropDownItems.Find(textList[loc4 + 1].Split('|')[0], true)[0];
                tsi.PerformClick();
            }
        }
        #endregion

        #region Trackbar Clicks
        private void Trackbar_Labels_Click(object sender, EventArgs e)
        {
            int trackNumber;
            string labelName = "";
            try { labelName = ((Label)sender).Name.Split('_')[1]; } catch { }
            int.TryParse(labelName, out trackNumber);
            trackBar1.Value = trackNumber;
            TrackBar1_Scroll(null, null);
        }// Merged all click events to one based on label Name.
        #endregion

        #region Other GUI Changes
        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                numericUpDown2.Enabled = true;
            }
            else
            {
                radioButton1.Enabled = false;
                radioButton2.Enabled = false;
                numericUpDown2.Enabled = false;
            }
        }
        private void ProgressUpdate(string[] info, int i = -1)
        {
            if (PT != PowerType.Infinite)
            {
                int perc = 0;
                if (i == -1)
                {
                    float total = 0f;
                    float.TryParse(info[3], out total);

                    float clicks = 0f;
                    float.TryParse(info[1], out clicks);

                    perc = ((int)clicks * 100) / (int)total;
                }
                else
                { perc = i; }
                if (perc < 0) { perc *= -1; }
                if (progressBar1.Value != perc)
                {
                    
                    progressBar1.Value = perc;
                    progressBar1.Update();
                    if (perc == 100)
                    {
                        Refresh();
                        Update();
                        Refresh();
                    }
                }
            }
        }
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            ListViewItem item = ListViewTryFindText(listView1, textBox1.Text);

            if (item != null)
            {
                button5.Enabled = true;
                button6.Enabled = true;
                button4.BackColor = item.BackColor;
            }
            else
            {
                button5.Enabled = false;
                button6.Enabled = false;
                button4.BackColor = ColorRandom();
            }
        }
        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            Color c1 = Color.Black;
            Color c2 = Color.Gray;

            lbl_10.ForeColor = c2;
            lbl_8.ForeColor = c2;
            lbl_6.ForeColor = c2;
            lbl_4.ForeColor = c2;
            lbl_2.ForeColor = c2;
            lbl_0.ForeColor = c2;

            button2.Text = "Stop";

            if (trackBar1.Value == 10)
            { lbl_10.ForeColor = c1; PT = PowerType.Infinite; button2.Text = "Slow Stop"; }
            else if (trackBar1.Value >= 8)
            { lbl_8.ForeColor = c1; PT = PowerType.Random; }
            else if (trackBar1.Value >= 6)
            { lbl_6.ForeColor = c1; PT = PowerType.Super; }
            else if (trackBar1.Value >= 4)
            { lbl_4.ForeColor = c1; PT = PowerType.Strong; }
            else if (trackBar1.Value >= 2)
            { lbl_2.ForeColor = c1; PT = PowerType.Average; }
            else if (trackBar1.Value >= 0)
            { lbl_0.ForeColor = c1; PT = PowerType.Weak; }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPluginToolStripMenuItem_Click(null, null);
        }
        private void ListViewSetGUI()
        {
            if (!moveWinningTicketToolStripMenuItem.Checked)
            {
                listView2.Visible = false;
                button8.Visible = false;
                listView3.Visible = true;
                button9.Visible = true;
                labelWinList.Text = "Winning List:";
            }
            else
            {
                listView2.Visible = true;
                button8.Visible = true;
                listView3.Visible = false;
                button9.Visible = false;
                labelWinList.Text = "Winning Pot:";
            }
            this.Refresh();
        }
        #endregion

        #region Supporting Functions
        public object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
        public void SetPropValue(object src, string propName, object value)
        {
            Type t = src.GetType();
            PropertyInfo p = t.GetProperty(propName);
            p.SetValue(src, value);
        }
        private Color ColorRandom()
        {
            Random rnd = new Random();
            return Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
        }
        public bool IsReadable(Color color1, Color color2)
        {
            return Math.Abs(color1.GetBrightness() - color2.GetBrightness()) >= 0.5f;
        }
        #endregion
    }
    public enum TicketUpdateType
    {
        Add,
        Subtract,
        Update
    }
}
