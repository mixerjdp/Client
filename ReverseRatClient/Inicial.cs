using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ReverseRatClient
{
    public partial class Inicial : Form
    {
        Miscelanea msc = new Miscelanea();
        public Inicial()
        {
            InitializeComponent();
        }

        private void Inicial_Load(object sender, EventArgs e)
        {
            Principal pc = new Principal();
            pc.Tag = "Principal";
            msc.MostrarMdiNuevo(this, pc, "Principal");
            
            
        }
    }
}
