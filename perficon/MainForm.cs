using Cave.Collections;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PerfIcon
{
    public partial class MainForm : Form
    {
        Stopwatch SwTimer = new Stopwatch();
        PerformanceCounter PCPhysicalDisk = null;
        MovingAverageFloat MAPhysicalDisk = new MovingAverageFloat();
        MovingAverageFloat MAGraph = new MovingAverageFloat();

        Random TestRandom = new Random();

        public MainForm()
        {
            InitializeComponent();
            MAPhysicalDisk.MaximumCount = 10;
            MAGraph.MaximumCount = 30;
            try
            {
                PCPhysicalDisk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Performance Counter. Please check your rights.");
                throw ex;
            }

            DrawGraph();
            notifyIcon.Visible = true;
            tMain.Enabled = true;
            SwTimer.Restart();
        }

        private void tMain_Tick(object sender, EventArgs e)
        {
            // get Performace, calculate Average
            MAPhysicalDisk.Add(PCPhysicalDisk.NextValue());
            if (SwTimer.ElapsedMilliseconds > 1000)
            {
                SwTimer.Reset();
                MAGraph.Add(MAPhysicalDisk.Average);
                DrawGraph();
                SwTimer.Start();
            }
        }


        private void DrawGraph()
        {
            Bitmap gBitmap = new Bitmap(32, 32);

            using (var graphics = Graphics.FromImage(gBitmap))
            {
                graphics.Clear(Color.Black);
                graphics.DrawRectangle(Pens.Gray, 0,0,31,31);
                int posX = 31 - MAGraph.Count;
                foreach (float value in MAGraph)
                {
                    if (posX > 30) { break; }
                    // clamp value
                    int y = (int)Math.Min(29f, 30 * value / 100f);
                    graphics.DrawLine(Pens.Green, posX, 30 - y, posX, 30);
                    posX++;
                }

            }

            IntPtr HICon = gBitmap.GetHicon();
            notifyIcon.Icon = Icon.FromHandle(HICon); ;
            notifyIcon.Text = String.Format("HDD:{0:0.##}%", MAPhysicalDisk.Average);


            using (var graphics = this.CreateGraphics())
            {
                graphics.DrawImage(gBitmap, ClientRectangle);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Visible = !hideOnMinimizeToolStripMenuItem.Checked;
            }
            else
            {
                this.Visible = true;
            }
        }

        private void hideOnMinimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainForm_Resize(sender, e);
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            showToolStripMenuItem_Click(sender, e);
        }
    }
}
