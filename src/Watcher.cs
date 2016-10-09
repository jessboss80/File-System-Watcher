/* Jesse Bosshardt 
 * 
 * Watcher.cs: 
 * This Windows Form watches a given directory, reports changes to the console, and writes changes to an SQLite database.  
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Collections;
using System.Media;

namespace Watcher
{

    public partial class Watcher : Form
    {
        private class EntryItem
        {
            public string Extension { get; set; }
            public string Filename { get; set; }
            public string Path { get; set; }
            public string Event { get; set; }
            public string Date { get; set; }

            public EntryItem(string extension, string filename, string path, string vent, string date)
            {
                Extension = extension;
                Filename = filename;
                Path = path;
                Event = vent;
                Date = date;
            }

        }

        delegate void AddItem(string text, EntryItem item);

        private FileSystemWatcher watcher;
        private SQLiteConnection connection;
        private ArrayList entryList;
        private Form queryForm;

        public Watcher()
        {
            InitializeComponent();
            entryList = new ArrayList();
            queryForm = new Query();
            queryForm.Owner = this;
        }

        private void Watcher_Load(object sender, EventArgs e)
        {          
            lstEvents.HorizontalScrollbar = true;          
            btnStop.Enabled = false;
            btnWrite.Enabled = false;
            stopWatchingToolStripMenuItem.Enabled = false;
            writeActivityToDatabaseToolStripMenuItem.Enabled = false;
            InitDatabase();
        }

        private void Watcher_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(watcher != null)
            {
                watcher.Dispose();
            }
            connection.Close();
        }

        #region Button Clicks

        private void btnView_Click(object sender, EventArgs e)
        {
                queryForm.Show();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            lblStatus.MaximumSize = new Size(this.Width - 50, 0);
            string path = txtDirectory.Text;
            lblWritten.Text = null;
            if (!Directory.Exists(path))
            {
                lblStatus.Text = "Invalid path. Please re-enter a valid path.";
            }
            else
            {
                WatchPath(path);
                lblStatus.Text = "Started watching " + cboExtension.Text + " files in " + path + " at " + DateTime.Now;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnView.Enabled = false;
                btnClear.Enabled = false;
                startWatchingToolStripMenuItem.Enabled = false;
                stopWatchingToolStripMenuItem.Enabled = true;
                viewDatabaseToolStripMenuItem.Enabled = false;
                clearDatabaseToolStripMenuItem.Enabled = false;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            string path = txtDirectory.Text;
            lblStatus.Text = "Stopped watching " + cboExtension.Text + " files in " + path;
            watcher.Dispose();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnView.Enabled = true;
            btnClear.Enabled = true;
            startWatchingToolStripMenuItem.Enabled = true;
            stopWatchingToolStripMenuItem.Enabled = false;
            viewDatabaseToolStripMenuItem.Enabled = true;
            clearDatabaseToolStripMenuItem.Enabled = true;

            if (entryList.Count > 0)
            {
                btnWrite.Enabled = true;
                writeActivityToDatabaseToolStripMenuItem.Enabled = true;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            entryList.Clear();
            lstEvents.Items.Clear();
            btnWrite.Enabled = false;
            writeActivityToDatabaseToolStripMenuItem.Enabled = false;
            lblWritten.Text = null;
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            foreach(EntryItem item in entryList)
            {
                string statement = "insert into changes (Extension, Filename, Path, Event, Date) values (@Extension, @Filename, @Path, @Event, @Date)";
                SQLiteCommand command = new SQLiteCommand(statement, connection);

                command.Parameters.AddWithValue("@Extension", item.Extension);
                command.Parameters.AddWithValue("@Filename", item.Filename);
                command.Parameters.AddWithValue("@Path", item.Path);
                command.Parameters.AddWithValue("@Event", item.Event);
                command.Parameters.AddWithValue("@Date", item.Date);

                command.ExecuteNonQuery();
            }

            entryList.Clear();
            lstEvents.Items.Clear();

            lblWritten.Text = "Write Successful!";

            btnWrite.Enabled = false;
            writeActivityToDatabaseToolStripMenuItem.Enabled = false;
        }

        #endregion

        /*WatchPath
         * Sets up and begins a file system watcher on a file path.
         */

        private void WatchPath(string path)
        {
            watcher = new FileSystemWatcher();

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Path = path;

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);
            if (!info.Name.Equals("log.db-journal", StringComparison.Ordinal) && !info.Name.Equals("log.db", StringComparison.Ordinal))
            {
                if (lstEvents.InvokeRequired)
                {
                    string text = info.Name + " " + e.FullPath + " " + e.ChangeType + " " + DateTime.Now;
                    EntryItem item = new EntryItem(info.Extension, info.Name, e.FullPath, e.ChangeType.ToString(), DateTime.Now.ToString());
                    AddToList(text, item);
                }
            }
        }


        /* AddToList
         * Add a new watched item to a list using a delegate to cross threads.
         */
        private void AddToList(string entry, EntryItem item)
        {
            if (lstEvents.InvokeRequired)
            {
                AddItem d = new AddItem(AddToList);
                this.Invoke(d, new object[] { entry, item });
            }
            else
            {
                if (cboExtension.Text == "ALL" || cboExtension.Text == item.Extension)
                {
                    lstEvents.Items.Add(entry);
                    entryList.Add(item);
                }
            }
        }




        private void InitDatabase()
        {
            string path = Directory.GetCurrentDirectory() + "\\log.db";
            if (!File.Exists(path))
            {
                SQLiteConnection.CreateFile("log.db");
                connection = new SQLiteConnection("Data Source=log.db;Version=3;");
                connection.Open();
                string createTable = "create table changes (Extension varchar(60), Filename varchar(100), Path varchar(10), Event varchar(20), Date varchar(40))";
                SQLiteCommand command = new SQLiteCommand(createTable, connection);
                command.ExecuteNonQuery();
                Console.WriteLine("SQLite database created");
            }
            else
            {
                connection = new SQLiteConnection("Data Source=log.db;Version=3;");
                connection.Open();
                Console.WriteLine("Connection to SQLite database established");
            }
        }




        #region Colors 

        /*Color Themes */
        private void grayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NullifyCheckMark();
            grayToolStripMenuItem.Checked = true;

            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;

            btnStart.BackColor = SystemColors.Control;
            btnStop.BackColor = SystemColors.Control;
            btnView.BackColor = SystemColors.Control;
            btnWrite.BackColor = SystemColors.Control;
            btnClear.BackColor = SystemColors.Control;

        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NullifyCheckMark();
            darkToolStripMenuItem.Checked = true;

            BackColor = Color.Black;
            ForeColor = Color.White;

            btnStart.BackColor = Color.DarkGray;
            btnStop.BackColor = Color.DarkGray;
            btnView.BackColor = Color.DarkGray;
            btnWrite.BackColor = Color.DarkGray;
            btnClear.BackColor = Color.DarkGray;
        }

        private void winterFlowerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NullifyCheckMark();
            winterFlowerToolStripMenuItem.Checked = true; ;

            BackColor = Color.White;
            ForeColor = Color.DeepPink;

            btnStart.BackColor = Color.WhiteSmoke;
            btnStop.BackColor = Color.WhiteSmoke;
            btnView.BackColor = Color.WhiteSmoke;
            btnWrite.BackColor = Color.WhiteSmoke;
            btnClear.BackColor = Color.WhiteSmoke;

        }

        private void springRushToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NullifyCheckMark();
            springRushToolStripMenuItem.Checked = true;

            BackColor = Color.LightGreen;
            ForeColor = Color.Blue;

            btnStart.BackColor = Color.BurlyWood;
            btnStop.BackColor = Color.BurlyWood;
            btnView.BackColor = Color.BurlyWood;
            btnWrite.BackColor = Color.BurlyWood;
            btnClear.BackColor = Color.BurlyWood;
        }

        private void deepOceanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NullifyCheckMark();
            deepOceanToolStripMenuItem.Checked = true;

            BackColor = Color.DarkBlue;
            ForeColor = Color.DeepSkyBlue;

            btnStart.BackColor = Color.DarkOrchid;
            btnStop.BackColor = Color.DarkOrchid;
            btnView.BackColor = Color.DarkOrchid;
            btnWrite.BackColor = Color.DarkOrchid;
            btnClear.BackColor = Color.DarkOrchid;
        }

        private void NullifyCheckMark()
        {
            grayToolStripMenuItem.Checked = false;
            darkToolStripMenuItem.Checked = false;
            winterFlowerToolStripMenuItem.Checked = false;
            deepOceanToolStripMenuItem.Checked = false;
            springRushToolStripMenuItem.Checked = false;
        }

        #endregion

        private void Watcher_FormClosing(object sender, FormClosingEventArgs e)
        {          
            SystemSounds.Asterisk.Play();
            if (MessageBox.Show("Would you like to write the recent activity to the database before you close?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                btnWrite.PerformClick();       
            }
        }

        #region Menu
        private void startWatchingToolStripMenuItem_Click(object sender, EventArgs e)
        {           
            btnStart.PerformClick();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void stopWatchingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnStop.PerformClick();
        }

        private void writeActivityToDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnWrite.PerformClick();
        }

        private void viewDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnView.PerformClick();
        }

        private void clearDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SystemSounds.Asterisk.Play();
            if (MessageBox.Show("Are you sure you want to clear the database?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string sql = "delete from changes";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
            }

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("File Watcher\nVersion 1.0.0\n\nHow To Use: \nSelect a directory and file extension to monitor, then click the Start button.\n" +
                "The activity will appear in the Watched Activity Window. \nAfter clicking the Stop button, you may Write To, View, and Manage the database.", "About", MessageBoxButtons.OK);
        }

        #endregion
    }

}
