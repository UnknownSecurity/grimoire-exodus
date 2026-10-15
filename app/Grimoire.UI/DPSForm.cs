using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Grimoire.UI
{
    public partial class DPSForm : DarkForm
    {
        public DPSForm()
        {
            InitializeComponent();
            timer1.Start();
        }

        public Dictionary<string, int> Damage = new Dictionary<string, int>();

        public Dictionary<string, int> DamagePerSecond = new Dictionary<string, int>();

        private void DPSForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (DamagePerSecond.Count == 0) return;
            long totalDamagePerSecond = DamagePerSecond.Values.Sum(value => (long)value);
            darkLabel1.Text = totalDamagePerSecond.ToString();
            chart1.Series.Clear();
            foreach (var kv in DamagePerSecond)
            {
                chart1.Series.Add(kv.Key);
                chart1.Series[kv.Key].Points.Add(kv.Value);
                chart1.Series[kv.Key].ChartType = SeriesChartType.Column;
            }
            DamagePerSecond.Clear();
        }

        private void DPSForm_Load(object sender, EventArgs e)
        {
            chart1.Series.Clear();
        }
    }
}
