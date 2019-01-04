using Cave.Collections;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PerfIcon
{
    /// <summary>
    /// Main Form, shows the Graph if visible
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Stopwatch for checking when to redraw the graph
        /// </summary>
        Stopwatch SwTimer = new Stopwatch();

        /// <summary>
        /// Performance counter for physical disk activity
        /// </summary>
        PerformanceCounter PCPhysicalDisk = null;

        /// <summary>
        /// holds first layer of average data of physical disk activity
        /// </summary>
        MovingAverageFloat MAPhysicalDisk = new MovingAverageFloat();

        /// <summary>
        /// holds second layer of data for drawing the graph
        /// </summary>
        MovingAverageFloat MAGraph = new MovingAverageFloat();

        /// <summary>
        /// Destroys a Icon Handle. Needs to be imported.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        /// <summary>
        /// initializes the main form
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            this.Visible = false;
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
            timerMain.Enabled = true;
            SwTimer.Restart();
        }

        /// <summary>
        /// main timer tick. gets performance counter data, calculates the average and redraws the graph if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerMain_Tick(object sender, EventArgs e)
        {
            // get Performace, calculate average
            MAPhysicalDisk.Add(PCPhysicalDisk.NextValue());
            if (SwTimer.ElapsedMilliseconds > 1000)
            {
                SwTimer.Reset();
                MAGraph.Add(MAPhysicalDisk.Average);
                DrawGraph();
                SwTimer.Start();
            }
        }

        /// <summary>
        /// draws the graph on the notify icon and on the form if visible
        /// </summary>
        private void DrawGraph()
        {
            Bitmap gBitmap = new Bitmap(32, 32);

            using (var graphics = Graphics.FromImage(gBitmap))
            {
                // black background with light gray border
                graphics.Clear(Color.Black);
                graphics.DrawRectangle(Pens.LightGray, 0, 0, 31, 31);
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

            // hold old icon handle
            IntPtr HOldIcon = notifyIcon.Icon?.Handle ?? IntPtr.Zero;
            IntPtr HICon = gBitmap.GetHicon();

            // set new icon
            notifyIcon.Icon = Icon.FromHandle(HICon); ;
            notifyIcon.Text = String.Format("HDD: {0:0.##}%", MAPhysicalDisk.Average);

            // destroy old icon
            DestroyIcon(HOldIcon);

            if (this.Visible)
            {
                using (var graphics = this.CreateGraphics())
                {
                    graphics.DrawImage(gBitmap, ClientRectangle);
                }
            }
        }

        /// <summary>
        /// context menu item to close the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// hides the form when minimizing if context menu item is set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// hides / shows the form if context menu item is set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideOnMinimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainForm_Resize(sender, e);
        }

        /// <summary>
        /// shows the form when context menu item is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        /// <summary>
        /// shows the form when notify icon is double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowToolStripMenuItem_Click(sender, e);
        }
    }
}
