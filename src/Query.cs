/* Jesse Bosshardt 
 * 
 * Query.cs: 
 * This Windows Form is a subform to Watcher.cs. It is used to manage the file watcher database.
 * 
 */


using System;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Media;

namespace Watcher
{
    public partial class Query : Form
    {
        private SQLiteConnection connection;

        public Query()
        {
            InitializeComponent();
        }

        private void Query_Load(object sender, EventArgs e)
        {
            connection = new SQLiteConnection("Data Source=log.db;Version=3;");
            connection.Open();
        }


        private void btnQuery_Click(object sender, EventArgs e)
        {
            dgvGrid.Rows.Clear();
            string sql;
            SQLiteCommand command;

            if(cboQuery.Text == "ALL")
            {
                sql = "select * from changes";
                command = new SQLiteCommand(sql, connection);
            }
            else
            {
                sql = "select * from changes where Extension = @event";
                command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@event", cboQuery.Text);
            }            
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                dgvGrid.Rows.Add(reader["Extension"], reader["Filename"], reader["Path"], reader["Event"], reader["Date"]);
            }
        }

        private void Query_FormClosing(object sender, FormClosingEventArgs e)
        {          
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            SystemSounds.Asterisk.Play();
            if (MessageBox.Show("Are you sure you want to clear the database?", "Warning!", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                dgvGrid.Rows.Clear();

                string sql = "delete from changes";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();

                lblCleared.Text = "*Database Cleared";
                lblCleared.Focus();
            }

        }

        private void lblCleared_Leave(object sender, EventArgs e)
        {
            lblCleared.Text = null;
        }
    }
}
