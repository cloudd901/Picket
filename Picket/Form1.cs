using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Picket
{
    public partial class Form1 : Form
    {
        public System.Timers.Timer ReverseListUpdateTimer;
        public PowerType PT = PowerType.Random;
        public MyReflection.ReflectionData Instance { get; set; }
        public List<AssemblyName> AssList { get; } = new List<AssemblyName>();
        public Form1()
        {
            InitializeComponent();

            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\plugins\");
            di.GetFiles("*.dll");
            foreach (FileInfo f in di.GetFiles("*.dll"))
            {
                AssList.Add(AssemblyName.GetAssemblyName(f.FullName));
                ToolStripMenuItem item = new ToolStripMenuItem(AssList.Last().Name, null, SetAssembly, AssList.Last().Name);
                pluginToolStripMenuItem.DropDownItems.Add(item);
            }
            Assembly assembly = Assembly.Load(AssList.FirstOrDefault(x => x.Name.Contains("Wheel")));
            LoadAssembly(assembly);
            TrackBar1_Scroll(null, null);
            button4.BackColor = ColorRandom();
            
        }

        private void SetAssembly(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.Load(AssList.FirstOrDefault(x => x.Name == ((ToolStripMenuItem)sender).Name));
            LoadAssembly(assembly);

        }

        private void LoadAssembly(Assembly assembly)
        {
            if (Instance != null)
            {
                Instance.Methods.dispose.Invoke(Instance.iRandomInstance, BindingFlags.Instance, null, new object[] { true }, System.Globalization.CultureInfo.CurrentCulture);
            }

            Instance = new MyReflection(this, assembly).Instance;

            Instance.toolProperties.GetType().GetProperty("ForceUniqueEntryColors").SetValue(Instance.toolProperties, true);
            Instance.toolProperties.GetType().GetProperty("ArrowPosition").SetValue(Instance.toolProperties, (int)ArrowLocation.Left);
            Instance.toolProperties.GetType().GetProperty("TextToShow").SetValue(Instance.toolProperties, (int)TextType.Name);
            Instance.iRandomInstance.GetType().GetProperty("AllowExceptions").SetValue(Instance.iRandomInstance, false);

            Instance.Methods.draw.Invoke(Instance.iRandomInstance, new object[] { 15, 30, 150 });

            UpdateEntryList();
        }
        private void EventStopCall(object entry)
        {
            ReverseListUpdateTimer_Clear();
            ReverseListUpdateTimer_Elapsed(null, null);

            if (label1.InvokeRequired)
            {
                label1.Invoke((MethodInvoker)delegate
                {
                    label1.Text = $"{ GetPropValue(entry, "Name") }";
                    label1.BackColor = (Color)GetPropValue(entry, "Aura");
                    label1.Refresh();
                    progressBar1.Value = 100;
                    progressBar1.Update();
                });
            }
            else
            {
                //label1.Text = $"Name: { GetPropValue(entry, "Name") }\r\nTicket: {GetPropValue(entry, "UniqueID")}";
                label1.Text = $"{ GetPropValue(entry, "Name") }";
                label1.BackColor = (Color)GetPropValue(entry, "Aura");
                label1.Refresh();
                progressBar1.Value = 100;
                progressBar1.Update();
            }
        }
        private void EventActionCall(object entry, string[] actionInfo)
        {
            if (ReverseListUpdateTimer == null)
            {
                ReverseListUpdateTimer = new System.Timers.Timer();
                ReverseListUpdateTimer.Elapsed += new ElapsedEventHandler(ReverseListUpdateTimer_Elapsed);
                //.2 second
                ReverseListUpdateTimer.Interval = 200;
                ReverseListUpdateTimer.Start();
            }

            try
            {
                if (label1.InvokeRequired)
                {
                    label1.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = $"{ GetPropValue(entry, "Name") }";
                        label1.BackColor = (Color)GetPropValue(entry, "Aura");
                        label1.Refresh();
                        ProgressUpdate(actionInfo);
                    });
                }
                else
                {
                    label1.Text = $"{ GetPropValue(entry, "Name") }";
                    label1.BackColor = (Color)GetPropValue(entry, "Aura");
                    label1.Refresh();
                    ProgressUpdate(actionInfo);
                }
            }
            catch
            { }
            
        }

        private void ReverseListUpdateTimer_Clear()
        {
            if (ReverseListUpdateTimer != null)
            {
                ReverseListUpdateTimer.Stop();
                ReverseListUpdateTimer.Dispose();
                ReverseListUpdateTimer = null;
            }
        }
        private void ReverseListUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (listView1.InvokeRequired)
            {
                listView1.Invoke((MethodInvoker)delegate
                {
                    ReverseLoadListView();
                });
            }
            else
            {
                ReverseLoadListView();
            }
            ReverseListUpdateTimer_Clear();
        }

        private void ProgressUpdate(string[] info)
        {
            if (PT != PowerType.Infinite)
            {
                float total = 0f;
                float.TryParse(info[3], out total);

                float clicks = 0f;
                float.TryParse(info[1], out clicks);

                int perc = 0;
                perc = ((int)clicks * 100) / (int)total;

                progressBar1.Value = perc;
                progressBar1.Update();
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
        private void Button1_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            new Thread(() => { Instance.Methods.start.Invoke(Instance.iRandomInstance, new object[] { (int)Direction.Clockwise, (int)PT, 5 }); }).Start();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Instance.Methods.stop.Invoke(Instance.iRandomInstance, null);
        }

        private void ListToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Button3_Click(object sender, EventArgs e) //add
        {
            Color c1 = button4.BackColor;
            Color c2 = Color.Black;
            bool check = Convert.ToBoolean(Instance.Methods.isReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
            if (!check) { c2 = Color.White; }

            string name = textBox1.Text.Trim();
            int cnt = (int)numericUpDown1.Value;

            ListViewItem item = listView1.FindItemWithText(name);
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
        }

        private void UpdateEntryList()
        {
            Instance.Methods.entriesClear.Invoke(Instance.iRandomInstance, new object[] { });
            foreach (ListViewItem item in listView1.Items)
            {
                int cnt = int.Parse(item.SubItems[1].Text);
                for (int i = 0; i < cnt; i++)
                {
                    Instance.Methods.entryAdd.Invoke(Instance.iRandomInstance, new object[] { NewEntry(item.Text, item.BackColor) });
                }
            }
            Instance.Methods.refresh.Invoke(Instance.iRandomInstance, new object[] { });
        }

        private Color ColorRandom()
        {
            Random rnd = new Random();
            return Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();
            if (result != DialogResult.Cancel)
            {
                button4.BackColor = colorDialog1.Color;
            }
        }

        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem item = listView1.SelectedItems[0];
            textBox1.Text = item.SubItems[0].Text;
            try { numericUpDown1.Value = int.Parse(item.SubItems[1].Text); } catch { }
            button4.BackColor = item.BackColor;
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            ListViewItem item = listView1.FindItemWithText(textBox1.Text, true, 0, false);
            if (item != null)
            {
                button5.Enabled = true;
                button6.Enabled = true;
            }
            else
            {
                button5.Enabled = false;
                button6.Enabled = false;
                button4.BackColor = ColorRandom();
            }
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            //Color c1 = button4.BackColor;
            //Color c2 = Color.Black;
            //bool check = Convert.ToBoolean(Instance.Methods.isReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
            //if (!check) { c2 = Color.White; }

            string name = textBox1.Text.Trim();
            int cnt = (int)numericUpDown1.Value;

            ListViewItem item = listView1.FindItemWithText(name, true, 0, false);
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

                //listView1.Items[loc].BackColor = c1;
                //listView1.Items[loc].ForeColor = c2;
            }
            //else
            //{
            //    for (int i = 0; i < numericUpDown1.Value; i++)
            //    {
            //        listView1.Items.Add(new ListViewItem(new string[] { name, cnt.ToString() }, 0, c2, c1, default));
            //    }
            //}
            UpdateEntryList();
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

        private void Button5_Click(object sender, EventArgs e)
        {
            Color c1 = button4.BackColor;
            Color c2 = Color.Black;
            bool check = Convert.ToBoolean(Instance.Methods.isReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
            if (!check) { c2 = Color.White; }

            string name = textBox1.Text.Trim();
            int cnt = 0;// (int)numericUpDown1.Value;

            ListViewItem item = listView1.FindItemWithText(name);
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
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            Instance.Methods.shuffleEntries.Invoke(Instance.iRandomInstance, new object[] { });
            ReverseLoadListView();
            Instance.Methods.draw.Invoke(Instance.iRandomInstance, new object[] { 15, 30, 150 });
        }

        private void ReverseLoadListView()
        {
            var entryList = CallEntryList();
            listView1.Items.Clear();

            foreach (dynamic entry in entryList)
            {
                Color c1 = entry.Aura;
                Color c2 = Color.Black;
                bool check = Convert.ToBoolean(Instance.Methods.isReadable.Invoke(Instance.iRandomInstance, new object[] { c1, c2 }));
                if (!check) { c2 = Color.White; }

                string name = entry.Name;
                int cnt = 1;

                ListViewItem item = listView1.FindItemWithText(name);
                if (item != null)
                {
                    int loc = listView1.Items.IndexOf(item);
                    int.TryParse(listView1.Items[loc].SubItems[1].Text, out cnt);
                    cnt ++;

                    listView1.Items[loc].SubItems[1].Text = cnt.ToString();
                    listView1.Items[loc].BackColor = c1;
                    listView1.Items[loc].ForeColor = c2;
                }
                else
                {
                    listView1.Items.Add(new ListViewItem(new string[] { name, cnt.ToString() }, 0, c2, c1, default));
                    TextBox1_TextChanged(null, null);
                }
            }
        }
        private dynamic CallEntryList()
        {
            var listType = typeof(List<>).MakeGenericType(Instance.EntryType);
            dynamic list = Activator.CreateInstance(listType);
            
            list = Instance.EntryList;
            return list;
        }
    }

}
