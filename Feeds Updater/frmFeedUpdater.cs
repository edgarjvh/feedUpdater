using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Feeds_Updater
{
    public partial class frmFeedUpdater : Form
    {
        wService.soccersoddsSoapClient ws;
        static string regDir = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\SportFeeds";
        string strConn = "";

        public frmFeedUpdater()
        {
            InitializeComponent();
            ws = new wService.soccersoddsSoapClient();
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void frmFeedUpdater_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                strConn = Registry.GetValue(regDir, "strConn", "").ToString();

                if (!strConn.Equals(""))
                {
                    using (SqlConnection conn = new SqlConnection(strConn))
                    {
                        conn.Open();

                        string query1 = "select * from countries";
                        SqlCommand cmd1 = new SqlCommand(query1, conn);
                        SqlDataAdapter da1 = new SqlDataAdapter(cmd1);
                        DataSet ds1 = new DataSet();
                        da1.Fill(ds1);

                        if (ds1.Tables[0].Rows.Count > 0)
                        {
                            // loop all countries looking for corresponding urls of odds, tvstations and livescores
                            for (int i = 0; i < ds1.Tables[0].Rows.Count; i++)
                            {
                                if (backgroundWorker1.CancellationPending)
                                {
                                    e.Cancel = true;
                                }

                                string query2 = "select * from feedurls where country_id = @1";
                                SqlCommand cmd2 = new SqlCommand(query2, conn);
                                cmd2.Parameters.AddWithValue("@1", (int)ds1.Tables[0].Rows[i]["country_id"]);
                                SqlDataAdapter da2 = new SqlDataAdapter(cmd2);
                                DataSet ds2 = new DataSet();
                                da2.Fill(ds2);

                                if (ds2.Tables[0].Rows.Count > 0)
                                {
                                    bool existsOdds = false;

                                    // loop the dataset of feed urls looking for a match with type "odds"
                                    for (int x = 0; x < ds2.Tables[0].Rows.Count; x++)
                                    {
                                        if (backgroundWorker1.CancellationPending)
                                        {
                                            e.Cancel = true;
                                        }

                                        // if there are odds, call the web service to retrieve the data
                                        if (ds2.Tables[0].Rows[x]["feed_type"].ToString().Equals("odds"))
                                        {
                                            ws.SaveSoccerOddsLivescore(ds2.Tables[0].Rows[x]["url"].ToString());
                                            existsOdds = true;
                                            break;
                                        }
                                    }

                                    if (!existsOdds) return;

                                    // loop the dataset of feed urls looking for a match with type "tvStations"
                                    for (int x = 0; x < ds2.Tables[0].Rows.Count; x++)
                                    {
                                        if (backgroundWorker1.CancellationPending)
                                        {
                                            e.Cancel = true;
                                        }

                                        // if there are tvstations, call the web service to retrieve the data
                                        if (ds2.Tables[0].Rows[x]["feed_type"].ToString().Equals("tvStations"))
                                        {
                                            ws.saveSoccerTvStations(ds2.Tables[0].Rows[x]["url"].ToString());
                                            break;
                                        }
                                    }

                                    // loop the dataset of feed urls looking for a match with type "livescore"
                                    for (int x = 0; x < ds2.Tables[0].Rows.Count; x++)
                                    {
                                        if (backgroundWorker1.CancellationPending)
                                        {
                                            e.Cancel = true;
                                        }

                                        // if there are livescores, call the web service to retrieve the data
                                        if (ds2.Tables[0].Rows[x]["feed_type"].ToString().Equals("livescore"))
                                        {
                                            ws.SaveSoccerOddsLivescore(ds2.Tables[0].Rows[x]["url"].ToString());
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Start();
            btnStart.Enabled = btnStop.Enabled;
            btnStop.Enabled = !btnStart.Enabled;
            startToolStripMenuItem.Enabled = btnStart.Enabled;
            stopToolStripMenuItem.Enabled = btnStop.Enabled;
            lblStatus.Text = "Feed Updater Started";
            lblStatus.BackColor = Color.LightGreen;
            Icon = Properties.Resources.iconStarted;
            NotFeedUpdater.Icon = Properties.Resources.iconStarted;

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            btnStop.Enabled = btnStart.Enabled;
            btnStart.Enabled = !btnStop.Enabled;
            startToolStripMenuItem.Enabled = btnStart.Enabled;
            stopToolStripMenuItem.Enabled = btnStop.Enabled;
            lblStatus.Text = "Feed Updater Stopped";
            lblStatus.BackColor = Color.LightCoral;
            Icon = Properties.Resources.iconStopped;
            NotFeedUpdater.Icon = Properties.Resources.iconStopped;
            backgroundWorker1.CancelAsync();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnStart_Click(null, null);
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnStop_Click(null, null);
        }

        private void frmFeedUpdater_FormClosing(object sender, FormClosingEventArgs e)
        {
            const string message = "Are you sure to exit?";
            const string caption = "Message";

            var result = MessageBox.Show(message, caption,
                             MessageBoxButtons.YesNo,
                             MessageBoxIcon.Question);

            e.Cancel = (result == DialogResult.No);
        }
    }
}
