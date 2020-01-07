using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Picket
{
    public partial class Form1 : Form
    {
        readonly object _timerLock = new object();
        private Thread ActionThread { get; set; }
        private Timer ReverseListTimer { get; set; }
        private PowerType PT { get; set; } = PowerType.Random;
        private MyReflection.ReflectionData Instance { get; set; }
        private List<AssemblyName> AssList { get; } = new List<AssemblyName>();

        public Form1()
        {
            InitializeComponent();

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
            ButtonStates(true);

            Instance = new MyReflection(this, assembly).Instance;

            Instance.ToolProperties.ForceUniqueEntryColors.SetValue(Instance.ToolPropertiesObj, true);
            Instance.ToolProperties.ArrowPosition.SetValue(Instance.ToolPropertiesObj, (int)ArrowLocation.Left);
            Instance.ToolProperties.TextToShow.SetValue(Instance.ToolPropertiesObj, (int)TextType.Name);
            Instance.Properties.AllowExceptions.SetValue(Instance.iRandomInstance, false);

            UpdateEntryList();

            Instance.Methods.Draw.Invoke(Instance.iRandomInstance, new object[] { 15, 30, 150 });
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
        private void EventStopCall(object entry)
        {
            DoEventAction(entry, null, 100);
        }
        private void EventActionCall(object entry, string[] actionInfo)
        {
            DoEventAction(entry, actionInfo);
            
        }
        private void DoEventAction(object entry, string[] actionInfo, int progressBar = -1)
        {
            ReverseListUpdateTimer_Set();

            try
            {
                if (label1.InvokeRequired)
                {
                    label1.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = $"{ GetPropValue(entry, "Name") }";
                        label1.BackColor = (Color)GetPropValue(entry, "Aura");
                        label1.Refresh();
                        ProgressUpdate(actionInfo, progressBar);
                    });
                }
                else
                {
                    label1.Text = $"{ GetPropValue(entry, "Name") }";
                    label1.BackColor = (Color)GetPropValue(entry, "Aura");
                    label1.Refresh();
                    ProgressUpdate(actionInfo, progressBar);
                }
            }
            catch
            { }
        }
        #endregion

        #region ListUpdate
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
            lock (_timerLock)
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
        private void UpdateEntryList()
        {
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
            ActionThread.Start();
        }// Start
        private void Button2_Click(object sender, EventArgs e)
        {
            Instance.Methods.Stop.Invoke(Instance.iRandomInstance, null);
        }// Stop
        private void Button3_Click(object sender, EventArgs e)
        {
            Color c1 = button4.BackColor;
            Color c2 = Color.Black;
            if (Instance != null)
            {
                bool check = Convert.ToBoolean(Instance.Methods.IsReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
                if (!check) { c2 = Color.White; }
            }
            string name = textBox1.Text.Trim();
            int cnt = (int)numericUpDown1.Value;

            ListViewItem item = ListViewTryFindText(listView1, name);
            if (item != null)
            {
                int loc = listView1.Items.IndexOf(item);
                int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                cnt += (int)numericUpDown1.Value;

                listView1.Items[loc].SubItems[1].Text = cnt.ToString();
                listView1.Items[loc].BackColor = c1;
                listView1.Items[loc].ForeColor = c2;
            }
            else
            {
                listView1.Items.Add(new ListViewItem(new string[] { name, cnt.ToString() }, 0, c2, c1, default));
                TextBox1_TextChanged(null, null);
            }
            UpdateEntryList();
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
            Color c1 = button4.BackColor;
            Color c2 = Color.Black;
            bool check = Convert.ToBoolean(Instance.Methods.IsReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
            if (!check) { c2 = Color.White; }

            string name = textBox1.Text.Trim();
            int cnt = 0;// (int)numericUpDown1.Value;

            ListViewItem item = ListViewTryFindText(listView1, name);
            if (item != null)
            {
                int loc = listView1.Items.IndexOf(item);
                int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                cnt = (int)numericUpDown1.Value;

                listView1.Items[loc].SubItems[1].Text = cnt.ToString();
                listView1.Items[loc].BackColor = c1;
                listView1.Items[loc].ForeColor = c2;
            }
            else
            {
                listView1.Items.Add(new ListViewItem(new string[] { name, cnt.ToString() }, 0, c2, c1, default));
                TextBox1_TextChanged(null, null);
            }
            UpdateEntryList();
        }// Update
        private void Button6_Click(object sender, EventArgs e)
        {
            string name = textBox1.Text.Trim();
            int cnt = (int)numericUpDown1.Value;

            ListViewItem item = ListViewTryFindText(listView1, name);
            if (item != null)
            {
                int loc = listView1.Items.IndexOf(item);
                int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                cnt -= (int)numericUpDown1.Value;

                if (cnt > 0)
                { listView1.Items[loc].SubItems[1].Text = cnt.ToString(); }
                else
                {
                    listView1.Items[loc].Remove();
                    TextBox1_TextChanged(null, null);
                }

            }
            UpdateEntryList();
        }// Subtract
        private void Button7_Click(object sender, EventArgs e)
        {
            Instance.Methods.ShuffleEntries.Invoke(Instance.iRandomInstance, new object[] { });
            ReverseUpdateEntryList();
            Instance.Methods.Draw.Invoke(Instance.iRandomInstance, new object[] { 15, 30, 150 });
        }// Shuffle
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
        }
        private void ClearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearPluginToolStripMenuItem_Click(null, null);
            ClearTicketListToolStripMenuItem_Click(null, null);
            textBox1.Text = "";
        }
        #endregion

        #region GUI Changes
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
                progressBar1.Value = perc;
                progressBar1.Update();
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
        #endregion


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

        private ListViewItem ListViewTryFindText(ListView listView, string text)
        {
            ListViewItem item = null;
            if (listView.Items.Count > 0)
            { item = listView.FindItemWithText(text, true, 0, false); }
            return item;
        }

        
    }

}
