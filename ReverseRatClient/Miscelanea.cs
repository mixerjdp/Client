using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SqlClient;
using System.Reflection;
using System.IO;
using System.Drawing.Printing;
using Microsoft.Win32;

namespace ReverseRatClient
{

    /// <summary>
    /// Clase para declarar variables globales
    /// </summary>
    static class Globales
    {
        public static string NombreColActual = "";
        public static string CadenaSQL = "";
        public static string IDColActual = "";
        public static string TipoColActual = "";
        public static double IVA_Actual = 0;
        public static bool AccionAutorizada = false;
        public static ArrayList ConfValor = new ArrayList();
        public static ArrayList ConfDesc = new ArrayList();
    }


    public class Miscelanea  // Por Juan Jos� Montserrat D�az Padilla
    {
        /// <summary>
        /// propiedad
        /// </summary>
        /// <remarks>comentario</remarks>
        /// <value>dummy</value>
        [Obsolete()]
        protected int Propejemplo
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

    

        public string SustituirApostrofes(string cadenaIni)
        {
            string cadenaFin = "";
            for (int x = 0; x < cadenaIni.Length; x++) // Sustituye apostrofes con guion (para codigos)
            {
                if (cadenaIni[x] == '\'')
                    cadenaFin += "-";
                else
                    cadenaFin += cadenaIni[x].ToString();
            }
            return cadenaFin;
        }
           

        public string FechaServidor(SqlCommand Comando, SqlConnection Conexion) // Obtiene fecha de la conexion sql al servidor
        {
            SqlDataReader Dreader;
            System.DateTime FechaHora;
            string Fecha = "";

            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();

            Conexion.Open();
            Comando.CommandText = "select Getdate() as Fecha";

            Dreader = Comando.ExecuteReader();
            if (Dreader.Read())
            {
                Fecha = Dreader[0].ToString();
            }
            Dreader.Close();
            Conexion.Close();
            if (Fecha == "")
            {
                Fecha = System.DateTime.Now.ToString();
            }

            FechaHora = Convert.ToDateTime(Fecha);
            // MessageBox.Show(FechaHora.ToString());
            return FechaHora.ToString();
        }




        // Parte una cadena y la almacena en un arraylist en el numero de segmentos especificados
        public void PartirCadena(String Cadena, ArrayList CadPar, int MaxCar)
        {
            int Longitud = Cadena.Length;
            float Segmentos = 0;
            int Sgtot = 0;
            int Contx = 0;
            int CMaxCar = 0;

            Segmentos = (float)Longitud / (float)MaxCar;
            if (Segmentos - Math.Truncate(Segmentos) > 0.01) // Dividir en el numero de segmentos superior inmediato si hay decimales
            {
                Segmentos++;
            }

            Sgtot = Convert.ToInt32(Segmentos);
            for (Contx = 0; Contx < Sgtot; Contx++)
            {
                if (CMaxCar + MaxCar > Cadena.Length) // Verificar que no sobrepase la longitud de la cadena
                {
                    MaxCar = Cadena.Length - CMaxCar;
                }
                CadPar.Add(Cadena.Substring(CMaxCar, MaxCar));
                CMaxCar += MaxCar;
            }
        }




        public object DevolverMDI(String Cadena) // Devuelve referencia a formulario MDI
        {                        
            int Contx;
          //  MessageBox.Show(Principal.ActiveForm.Name);
            for (Contx = 0; Contx < Principal.ActiveForm.MdiChildren.Length; Contx++)
            {
                if (Principal.ActiveForm.MdiChildren[Contx].Name == Cadena)
                    return Principal.ActiveForm.MdiChildren[Contx];                
            }
            return null;
        }


        public bool MostrarMdi(Form Padre, Form FormMdi, String Cadena, string vtag)
        {
            // Verifica todos los forms hijos para mostrar solo nuevos elementos y no crear repetidos
            int Contx;
            bool Encontrado = false;

            for (Contx = 0; Contx < Padre.MdiChildren.Length; Contx++)
            {
                if (Padre.MdiChildren[Contx].Name == Cadena && Convert.ToString(Padre.MdiChildren[Contx].Tag) == vtag)
                {
                    Encontrado = true;
                    Padre.MdiChildren[Contx].Activate();
                    Padre.MdiChildren[Contx].WindowState = FormWindowState.Normal;
                    Padre.MdiChildren[Contx].Visible = true;
                }
            }
            if (Encontrado == false)
            {
                FormMdi.Name = Cadena;
                FormMdi.MdiParent = Padre;
                FormMdi.Show();
            }
            return Encontrado;
        }



        public bool MostrarMdiNuevo(Form Padre, Form FormMdi, String Cadena) // Muestra una ventana MDI hija
        {
            // Verifica todos los forms hijos para mostrar solo nuevos elementos y no crear repetidos
            int Contx;
            bool Encontrado = false;
            
            if (Encontrado == false)
            {
                FormMdi.Name = Cadena;
                FormMdi.MdiParent = Padre;
                FormMdi.Show();
            }
            return Encontrado;
        }

     

        public void ComandoParametros(GroupBox Gbx, SqlCommand Comando)
        {
            // Asigna autom�ticamente par�metros de un comando SQL 
            string CadTag = "";
      
            foreach (Control Ctr in Gbx.Controls)
            {
                TextBox Txt = new TextBox();   // Para cualquier tipo de parametro descriptivo
                if (Ctr.GetType().Name == "TextBox")
                {
                    Txt = (TextBox)Ctr;
                    CadTag = Convert.ToString(Txt.Tag);
                    if (CadTag != "")
                    {
                        CadTag = CadTag.ToString().Split('|')[0];
                        if (CadTag.Length > 0)
                            Comando.Parameters[CadTag].Value = Txt.Text.Trim();
                    }
                }

                CheckBox Chk = new CheckBox();   // Para cualquier tipo de parametro descriptivo
                if (Ctr.GetType().Name == "CheckBox")
                {
                    Chk = (CheckBox)Ctr;
                    CadTag = Convert.ToString(Chk.Tag);
                    if (CadTag != "")
                    {
                        CadTag = CadTag.ToString().Split('|')[0];
                        if (CadTag.Length > 0)
                            Comando.Parameters[CadTag].Value = Chk.Checked.ToString();
                    }
                }
                

                MaskedTextBox mTxt = new MaskedTextBox();   // Para parametros como telefonos
                if (Ctr.GetType().Name == "MaskedTextBox")
                {
                    mTxt = (MaskedTextBox)Ctr;
                    CadTag = Convert.ToString(mTxt.Tag);
                    if (CadTag != "")
                    {
                        CadTag = CadTag.ToString().Split('|')[0];
                        if (CadTag.Length > 0)
                            Comando.Parameters[CadTag].Value = mTxt.Text;
                    }
                }

                ComboBox Cmbx = new ComboBox();                
                if (Ctr.GetType().Name == "ComboBox")
                {
                    Cmbx = (ComboBox)Ctr;
                    CadTag = Convert.ToString(Cmbx.Tag);
                    if (CadTag != "")
                    {
                        CadTag = Cmbx.Tag.ToString().Split('|')[0];
                        if (CadTag.Length > 0)
                            Comando.Parameters[CadTag.ToString()].Value = Cmbx.Text;
                    }
                }

                DateTimePicker Dtp = new DateTimePicker();
                if (Ctr.GetType().Name == "DateTimePicker") 
                {
                    Dtp = (DateTimePicker)Ctr;
                    CadTag = Convert.ToString(Dtp.Tag);
                    if (CadTag != "")
                    {
                        CadTag = Dtp.Tag.ToString().Split('|')[0];
                        if (CadTag.Length > 0)
                            Comando.Parameters[CadTag.ToString()].Value = Dtp.Value.ToString();
                    }
                }
            }
        }


        public void AsignarLector(GroupBox Gbx, SqlDataReader Lector)
        {
            // Asigna los valores devueltos por un lector de datos a un comando 
            string CadTag = "";
            int ValCad, Contador;

            foreach (Control Ctr in Gbx.Controls)
            {
                TextBox Txt = new TextBox();
                if (Ctr.GetType().Name == "TextBox") // Para cajas de texto
                {
                    Txt = (TextBox)Ctr;
                    CadTag = Convert.ToString(Txt.Tag);
                    if (CadTag != "")
                    {
                        Contador = 0;
                        foreach (string Cadtoken in Txt.Tag.ToString().Split('|'))
                            Contador++;
                        if (Contador > 1)
                        {
                            CadTag = Txt.Tag.ToString().Split('|')[1];
                            if (CadTag.Length > 0)
                            {
                                ValCad = Convert.ToInt32(CadTag.ToString());
                                Txt.Text = Lector[ValCad].ToString().Trim();
                            }
                        }

                    }
                }


                MaskedTextBox mTxt = new MaskedTextBox();
                if (Ctr.GetType().Name == "MaskedTextBox") // Para cajas de texto
                {
                    mTxt = (MaskedTextBox)Ctr;
                    CadTag = Convert.ToString(mTxt.Tag);
                    if (CadTag != "")
                    {
                        Contador = 0;
                        foreach (string Cadtoken in mTxt.Tag.ToString().Split('|'))
                            Contador++;
                        if (Contador > 1)
                        {
                            CadTag = mTxt.Tag.ToString().Split('|')[1];
                            if (CadTag.Length > 0)
                            {
                                ValCad = Convert.ToInt32(CadTag.ToString());
                                mTxt.Text = Lector[ValCad].ToString().Trim();
                            }
                        }
                    }
                }

                CheckBox Chk = new CheckBox();
                if (Ctr.GetType().Name == "CheckBox") // Para cajas de verificacion
                {
                    Chk = (CheckBox)Ctr;
                    CadTag = Convert.ToString(Chk.Tag);
                    if (CadTag != "")
                    {
                        Contador = 0;
                        foreach (string Cadtoken in Chk.Tag.ToString().Split('|'))
                            Contador++;
                        if (Contador > 1)
                        {
                            CadTag = Chk.Tag.ToString().Split('|')[1];
                            if (CadTag.Length > 0)
                            {
                                ValCad = Convert.ToInt32(CadTag.ToString());
                                Chk.Checked = Convert.ToBoolean(Lector[ValCad]);
                            }
                        }
                    }
                }

                DateTimePicker Dtp = new DateTimePicker();
                if (Ctr.GetType().Name == "DateTimePicker") // Para cajas de texto
                {
                    Dtp = (DateTimePicker)Ctr;
                    CadTag = Convert.ToString(Dtp.Tag);
                    if (CadTag != "")
                    {
                        Contador = 0;
                        foreach (string Cadtoken in Dtp.Tag.ToString().Split('|'))
                            Contador++;
                        if (Contador > 1)
                        {
                            CadTag = Dtp.Tag.ToString().Split('|')[1];
                            if (CadTag.Length > 0)
                            {
                                ValCad = Convert.ToInt32(CadTag.ToString());
                                Dtp.Value = Convert.ToDateTime(Lector[ValCad].ToString().Trim());
                            }
                        }
                    }
                }

                ComboBox Cbx = new ComboBox();
                if (Ctr.GetType().Name == "ComboBox") // Para COMBOBOXES
                {
                    Cbx = (ComboBox)Ctr;
                    CadTag = Convert.ToString(Cbx.Tag);
                    if (CadTag != "")
                    {
                        Contador = 0;
                        foreach (string Cadtoken in Cbx.Tag.ToString().Split('|'))
                            Contador++;
                        if (Contador > 1)
                        {
                            CadTag = Cbx.Tag.ToString().Split('|')[1];
                            if (CadTag.Length > 0)
                            {
                                ValCad = Convert.ToInt32(CadTag.ToString());                                
                                Cbx.SelectedIndex = Cbx.FindStringExact(Convert.ToString(Lector[ValCad].ToString().Trim()));
                            }
                        }
                    }
                }


            }
        }

        public void Limpia(Panel Pnl) // Limpia controles dentro de un groupbox
        {
            foreach (Control Ctr in Pnl.Controls)
            {
                TextBox Txt = new TextBox();
                if (Ctr.GetType().Name == "TextBox")
                {
                    Txt = (TextBox)Ctr;
                    Txt.Text = "";
                }

                MaskedTextBox mTxt = new MaskedTextBox();
                if (Ctr.GetType().Name == "MaskedTextBox")
                {
                    mTxt = (MaskedTextBox)Ctr;
                    mTxt.Text = "";
                }

                CheckBox Chk = new CheckBox();
                if (Ctr.GetType().Name == "CheckBox")
                {
                    Chk = (CheckBox)Ctr;
                    Chk.Checked = false;
                }

                ListView Lvw = new ListView();
                if (Ctr.GetType().Name == "ListView")
                {
                    Lvw = (ListView)Ctr;
                    Lvw.Items.Clear();
                }

                DataGridView Dvw = new DataGridView();
                if (Ctr.GetType().Name == "DataGridView")
                {
                    Dvw = (DataGridView)Ctr;
                    Dvw.Rows.Clear();
                }

            }
        }



        public void Limpia(GroupBox Gbx) // Limpia controles dentro de un groupbox
        {
            foreach (Control Ctr in Gbx.Controls)
            {
                TextBox Txt = new TextBox();
                if (Ctr.GetType().Name == "TextBox")
                {
                    Txt = (TextBox)Ctr;
                    Txt.Text = "";
                }

                MaskedTextBox mTxt = new MaskedTextBox();
                if (Ctr.GetType().Name == "MaskedTextBox")
                {
                    mTxt = (MaskedTextBox)Ctr;
                    mTxt.Text = "";
                }

                CheckBox Chk = new CheckBox();
                if (Ctr.GetType().Name == "CheckBox")
                {
                    Chk = (CheckBox)Ctr;
                    Chk.Checked = false;
                }

                ListView Lvw = new ListView();
                if (Ctr.GetType().Name == "ListView")
                {
                    Lvw = (ListView)Ctr;
                    Lvw.Items.Clear();
                }

                DataGridView Dvw = new DataGridView();
                if (Ctr.GetType().Name == "DataGridView")
                {
                    Dvw = (DataGridView)Ctr;
                    Dvw.Rows.Clear();
                }
            }
        }



        public void ManejaFocos(GroupBox Caja)
        {
            // Resalta controles en foco al presionar tecla
            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();                

                Cnt = Ctr;
                if (Cnt.GetType().Name.Trim() == "TextBox") // NO Etiquetas
                {
                    TextBox Txt = new TextBox();
                    Txt = (TextBox)Cnt;

                    if (Txt.Focused == true)                        
                        Txt.BackColor = Color.DodgerBlue;
                    else
                    {
                        if (Txt.ReadOnly == true)                        
                            Txt.BackColor = SystemColors.Control;
                        else
                            Txt.BackColor = SystemColors.Window;
                    }
                   
                }                    
                else if (Cnt.GetType().Name.Trim() == "MaskedTextBox") // maskedtextbox
                {
                    MaskedTextBox MTxt = new MaskedTextBox();
                    MTxt = (MaskedTextBox)Cnt;

                    if (MTxt.Focused == true)
                        MTxt.BackColor = Color.DodgerBlue;
                    else
                    {
                        if (MTxt.ReadOnly == true)
                            MTxt.BackColor = SystemColors.Control;
                        else
                            MTxt.BackColor = SystemColors.Window;
                    }

                }
                else if (Cnt.GetType().Name.Trim() == "ComboBox")
                {

                    ComboBox Cbx = new ComboBox();
                    Cbx = (ComboBox)Cnt;
                    
                    if (Cbx.Focused == true)
                    {                        
                        Cbx.FlatStyle = FlatStyle.Popup;
                        Cbx.Font = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
                    }
                    else
                    {                        
                        Cbx.FlatStyle = FlatStyle.Standard;
                        Cbx.Font = new System.Drawing.Font("Tahoma", 8);
                    }
                }
                else if (Cnt.GetType().Name.Trim() == "Button")
                {

                    Button Btn = new Button();
                    Btn = (Button)Cnt;

                    if (Btn.Focused == true)
                    {
                        Btn.BackColor = SystemColors.Highlight;
                    }
                    else
                    {
                        Btn.BackColor = SystemColors.Control;

                    }
                }
                else if (Cnt.GetType().Name.Trim() == "CheckBox")
                {
                    CheckBox Chk = new CheckBox();
                    Chk = (CheckBox)Cnt;

                    if (Chk.Focused == true)
                    {
                        Chk.BackColor = Color.DodgerBlue;
                    }
                    else
                    {
                        Chk.BackColor = SystemColors.Control;

                    }
                }
                else if (Cnt.GetType().Name.Trim() == "RadioButton")
                {
                    RadioButton Rbt = new RadioButton();
                    Rbt = (RadioButton)Cnt;

                    if (Rbt.Focused == true)
                    {
                        Rbt.BackColor = Color.DodgerBlue;
                    }
                    else
                    {
                        Rbt.BackColor = SystemColors.Control;
                    }
                }
            }

        }


        public void ManejaFocos(Panel Caja)
        {
            // Resalta controles en foco al presionar tecla
            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();

                Cnt = Ctr;
                if (Cnt.GetType().Name.Trim() == "TextBox") // NO Etiquetas
                {
                    TextBox Txt = new TextBox();
                    Txt = (TextBox)Cnt;

                    if (Txt.Focused == true)
                        Txt.BackColor = Color.DodgerBlue;
                    else
                    {
                        if (Txt.ReadOnly == true)
                            Txt.BackColor = SystemColors.Control;
                        else
                            Txt.BackColor = SystemColors.Window;
                    }

                }
                else if (Cnt.GetType().Name.Trim() == "MaskedTextBox") // maskedtextbox
                {
                    MaskedTextBox MTxt = new MaskedTextBox();
                    MTxt = (MaskedTextBox)Cnt;

                    if (MTxt.Focused == true)
                        MTxt.BackColor = Color.DodgerBlue;
                    else
                    {
                        if (MTxt.ReadOnly == true)
                            MTxt.BackColor = SystemColors.Control;
                        else
                            MTxt.BackColor = SystemColors.Window;
                    }

                }
                else if (Cnt.GetType().Name.Trim() == "ComboBox")
                {

                    ComboBox Cbx = new ComboBox();
                    Cbx = (ComboBox)Cnt;

                    if (Cbx.Focused == true)
                    {
                        Cbx.FlatStyle = FlatStyle.Popup;
                        Cbx.Font = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
                    }
                    else
                    {
                        Cbx.FlatStyle = FlatStyle.Standard;
                        Cbx.Font = new System.Drawing.Font("Tahoma", 8);
                    }
                }
                else if (Cnt.GetType().Name.Trim() == "Button")
                {

                    Button Btn = new Button();
                    Btn = (Button)Cnt;

                    if (Btn.Focused == true)
                    {
                        Btn.BackColor = SystemColors.Highlight;
                    }
                    else
                    {
                        Btn.BackColor = SystemColors.Control;

                    }
                }
                else if (Cnt.GetType().Name.Trim() == "CheckBox")
                {

                    CheckBox Chk = new CheckBox();
                    Chk = (CheckBox)Cnt;

                    if (Chk.Focused == true)
                    {
                        Chk.BackColor = Color.DodgerBlue;
                    }
                    else
                    {
                        Chk.BackColor = SystemColors.Control;

                    }
                }
                else if (Cnt.GetType().Name.Trim() == "RadioButton")
                {
                    RadioButton Rbt = new RadioButton();
                    Rbt = (RadioButton)Cnt;

                    if (Rbt.Focused == true)
                    {
                        Rbt.BackColor = Color.DodgerBlue;
                    }
                    else
                    {
                        Rbt.BackColor = SystemColors.Control;
                    }
                }

            }

        }




        public void ManejaTabs(GroupBox Caja) // Pasa al siguiente control del TabIndex mediante la detecci�n del elemento actual en foco
        {
            TextBox Txt = new TextBox();
            RichTextBox Rch = new RichTextBox();
            Label Lbl = new Label();
            int ActualTab = 0, ContFoco = 0, TxtLim, TxtAct;

            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();
                Cnt = Ctr;
                if (Cnt.Focused == true) // Encuentra al TabIndex del TextBox Focused
                {
                    if (Ctr.GetType().Name == "TextBox")
                    {
                        Txt = (TextBox)Ctr;
                        if (Txt.Multiline == true) // Si es TextBox multilinea no realiza nada                                                 
                            return;
                    }
                    if (Ctr.GetType().Name == "RichTextBox")
                    {
                        Rch = (RichTextBox)Ctr;
                        if (Rch.Multiline == true) // Si es RichTextBox multilinea no realiza nada                                                 
                            return;
                    }

                    ActualTab = Cnt.TabIndex;
                    ContFoco++;
                    break;
                }
            }

            if (ContFoco == 0) // No hay elemento en foco, abandona la funci�n
                return;

            TxtLim = 999999; // Limite te�rico para el m�ximo de TABS
            foreach (Control Ctr in Caja.Controls) // Encontrar siguiente TAB
            {
                Control Cnt = new Control();
                Cnt = Ctr;

                if (Cnt.GetType().Name.Trim() != "Label") // NO Etiquetas
                {
                    // Validando que debe cumplir con estar activo y visible para ser un tab siguiente v�lido
                    if (Cnt.TabIndex > ActualTab && Cnt.Visible == true && Cnt.Enabled == true && Cnt.TabStop == true)
                    {
                        TxtAct = Cnt.TabIndex;
                        if (TxtAct < TxtLim)
                            TxtLim = TxtAct;
                    }
                }
            }
            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();

                Cnt = Ctr;
                if (Cnt.GetType().Name.Trim() != "Label") // NO Etiquetas
                {
                    if (Cnt.TabIndex == TxtLim && Cnt.Visible == true && Cnt.Enabled == true && Cnt.TabStop == true)
                    {
                        Cnt.Focus(); // Foco al control destino                                              
                        break;
                    }
                }
            }
        }


        public void ManejaTabs(Panel Caja) // Pasa al siguiente control del TabIndex mediante la detecci�n del elemento actual en foco
        {
            TextBox Txt = new TextBox();
            RichTextBox Rch = new RichTextBox();
            Label Lbl = new Label();
            int ActualTab = 0, ContFoco = 0, TxtLim, TxtAct;

            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();
                Cnt = Ctr;
                if (Cnt.Focused == true) // Encuentra al TabIndex del TextBox Focused
                {
                    if (Ctr.GetType().Name == "TextBox")
                    {
                        Txt = (TextBox)Ctr;
                        if (Txt.Multiline == true) // Si es TextBox multilinea no realiza nada                                                 
                            return;
                    }
                    if (Ctr.GetType().Name == "RichTextBox")
                    {
                        Rch = (RichTextBox)Ctr;
                        if (Rch.Multiline == true) // Si es RichTextBox multilinea no realiza nada                                                 
                            return;
                    }

                    ActualTab = Cnt.TabIndex;
                    ContFoco++;
                    break;
                }
            }

            if (ContFoco == 0) // No hay elemento en foco, abandona la funci�n
                return;

            TxtLim = 999999; // Limite te�rico para el m�ximo de TABS
            foreach (Control Ctr in Caja.Controls) // Encontrar siguiente TAB
            {
                Control Cnt = new Control();
                Cnt = Ctr;

                if (Cnt.GetType().Name.Trim() != "Label") // NO Etiquetas
                {
                    // Validando que debe cumplir con estar activo y visible para ser un tab siguiente v�lido
                    if (Cnt.TabIndex > ActualTab && Cnt.Visible == true && Cnt.Enabled == true && Cnt.TabStop == true)
                    {
                        TxtAct = Cnt.TabIndex;
                        if (TxtAct < TxtLim)
                            TxtLim = TxtAct;
                    }
                }
            }
            foreach (Control Ctr in Caja.Controls)
            {
                Control Cnt = new Control();

                Cnt = Ctr;
                if (Cnt.GetType().Name.Trim() != "Label") // NO Etiquetas
                {
                    if (Cnt.TabIndex == TxtLim)
                    {
                        Cnt.Focus(); // Foco al control destino                                              
                        break;
                    }
                }
            }
        }


        public void BorrarFila(bool Confirmacion, DataGridView Dvw)
        {
            if (Dvw.SelectedRows.Count > 0)
            {
                if (Confirmacion == true)
                {
                    if (MessageBox.Show("�Desea borrar el elemento seleccionado de la lista?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Dvw.Rows.Remove(Dvw.Rows[Dvw.SelectedRows[0].Index]);
                    }
                }
                else
                {
                    Dvw.Rows.Remove(Dvw.Rows[Dvw.SelectedRows[0].Index]);
                }
            }
            else
                MessageBox.Show("No hay elemento seleccionado", "Error");
        }



        public String ObtenerCampo(String Objetivo, String Campo, String Parametro, String Tabla, System.Data.SqlClient.SqlCommand Comando, SqlConnection Conexion)
        //Funci�n para obtener una cadena referenciando a un campo con determinado par�metro
        {
            SqlDataReader dreader;
            String Valor = "-1";
            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();
            Conexion.Open();
            Comando.CommandText = "Select " + Objetivo + " from " + Tabla + " where " + Campo + " = " + "'" + Parametro + "'";  //M�ximo valor
            dreader = Comando.ExecuteReader();
            if (dreader.Read())
                Valor = dreader[0].ToString().Trim();
            dreader.Close();
            Conexion.Close();

            return Valor; // Cadena                  
        }

        public String ObtenerCampo(String Objetivo, String Campo, String Parametro, String Tabla, System.Data.SqlClient.SqlCommand Comando, SqlConnection Conexion, String ParamAdic)
        {   //Sobrecargada incluyendo restricciones adicionales en el WHERE
            SqlDataReader dreader;
            String Valor = "-1";
            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();
            Conexion.Open();
            Comando.CommandText = "Select " + Objetivo + " from " + Tabla + " where " + Campo + " = " + "'" + Parametro + "'" + ParamAdic;  //M�ximo valor
            dreader = Comando.ExecuteReader();
            if (dreader.Read())
                Valor = dreader[0].ToString().Trim();
            dreader.Close();
            Conexion.Close();

            return Valor; // Cadena                  
        }


        public int MaxValor(String ID, String Tabla, SqlCommand Comando, SqlConnection Conexion)
        {
            // Funci�n para obtener el valor maximo de un ID en una tabla
            SqlDataReader dreader;
            int Valor = 0;

            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();

            Conexion.Open();
            Comando.CommandText = "Select max(" + ID + ") from " + Tabla;  // M�ximo valor
            dreader = Comando.ExecuteReader();
            if (dreader.Read() && dreader.IsDBNull(0) == false) // Comprueba que no sea nulo el valor
                Valor = Convert.ToInt32(dreader[0].ToString(), 10);

            dreader.Close();
            Conexion.Close();

            return Valor; // Max. Entero   
        }


        public int MaxValor(String ID, String Tabla, SqlCommand Comando, SqlConnection Conexion, string Adicional)
        {
            // maximo valor de una tabla, sobrecargada para soportar argumentos extras
            SqlDataReader dreader;
            int Valor = 0;

            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();

            Conexion.Open();
            Comando.CommandText = "Select max(" + ID + ") from " + Tabla + Adicional;  // M�ximo valor
            dreader = Comando.ExecuteReader();
            if (dreader.Read() && dreader.IsDBNull(0) == false) // Comprueba que no sea nulo el valor
                Valor = Convert.ToInt32(dreader[0].ToString(), 10);

            dreader.Close();
            Conexion.Close();

            return Valor; // Max. Entero   
        }


        public bool Vacia(String ID, String Tabla, SqlCommand Comando, SqlConnection Conexion)
        { // Comprueba si una tabla sql esta vacia o contiene datos
            SqlDataReader dreader;
            int Valor = 0;

            if (Conexion.State != System.Data.ConnectionState.Closed)
                Conexion.Close();

            Conexion.Open();
            Comando.CommandText = "Select count(" + ID + ") from " + Tabla;  // M�ximo valor
            dreader = Comando.ExecuteReader();
            if (dreader.Read() && dreader.IsDBNull(0) == false) // Comprueba que no sea nulo el valor
                Valor = Convert.ToInt32(dreader[0].ToString(), 10);

            dreader.Close();
            Conexion.Close();

            return (Valor == 0) ? true : false; // Max. Entero   
        }


        // Justificacion para escritura en un StreamWriter
        public void ImpEspacios(int NoEspacios, System.IO.StreamWriter Var, string Texto, bool Direccion)
        {
            // Direccion: just a la izq - true, a la derecha false 
            if (Direccion) Var.Write(Texto);
            for (int x = 0; x < NoEspacios - Texto.Length; x++)
                Var.Write(" ");
            if (!Direccion) Var.Write(Texto);
        }


        public void FocoEnter(Control Objeto, System.Windows.Forms.KeyEventArgs e)
        {
            //Foco a un objeto al darle enter
            if (e.KeyCode == Keys.Return)   // Enter
                Objeto.Focus();
        }


        public void EntradaTels(TextBox Txt)
        {    //    Restringe entradas de tel�fonos en campos textbox
            Int16 x;
            for (x = 0; x < Txt.Text.Length; x++)
            {
                if (Txt.Text[x].ToString() != "-" && Char.IsDigit(Txt.Text[x]) == false)
                {
                    Txt.Text = Txt.Text.Substring(1, Txt.Text.Length - 1); //Mid(Txt.Text, 1, Txt.Text.Length - 1);
                    Txt.SelectionStart = Txt.Text.Length;
                    return;
                }
            }
        }


        public void EntradaNums(TextBox Txt)
        {    //   Restringe entradas num�ricas en campos textbox
            Int16 x;
            for (x = 0; x < Txt.Text.Length; x++)
            {
                if (Txt.Text[x].ToString() != "-" && Txt.Text[x].ToString() != "," && Txt.Text[x].ToString() != "." && Char.IsDigit(Txt.Text[x]) == false)
                {
                    Txt.Text = Txt.Text.Substring(0, Txt.Text.Length - 1);
                    Txt.SelectionStart = Txt.Text.Length;
                    return;
                }
            }
        }

        public void EntradaNumsCS(TextBox Txt)
        {    //   Restringe entradas num�ricas con SIGNO en campos textbox
            Int16 x;
            for (x = 0; x < Txt.Text.Length; x++)
            {
                if (Txt.Text[x].ToString() != "." && Char.IsDigit(Txt.Text[x]) == false && Txt.Text[x].ToString() != "-")
                {
                    Txt.Text = Txt.Text.Substring(0, Txt.Text.Length - 1);
                    Txt.SelectionStart = Txt.Text.Length;
                    return;
                }
            }
        }

        public void EntradaNumsI(TextBox Txt)
        {    //  Restringe entradas num�ricas enteras en campos textbox
            Int16 x;
            for (x = 0; x < Txt.Text.Length; x++)
            {
                if (Char.IsDigit(Txt.Text[x]) == false)
                {
                    Txt.Text = Txt.Text.Substring(0, Txt.Text.Length - 1);
                    Txt.SelectionStart = Txt.Text.Length;
                    return;
                }
            }
        }


        public Int16 EncuentraCaracter(String Cadena, String Caracter)
        {
            // Encontrar caracter especificado en una cadena (similar a instr())
            Int16 Cnt, Cntc = 0;

            for (Cnt = 0; Cnt < Cadena.Length; Cnt++)
            {
                if (Caracter[0].ToString() == Cadena[Cnt].ToString())
                    Cntc += 1;
            }
            return Cntc;
        }

        public String FechasALetra(String Fecha)
        {
            // Verifica fechas y las convierte a letra dependiendo de la forma de entrada
            String ValorDev, ValorDev2;
            Int16 Mes;
            String MesLetra = "";
            String Car1 = "/", Car2 = "-";
            Char Car;

            try
            {
                Fecha = Fecha.Split(' ')[0];
            }
            catch (Exception Ex)
            {
              Console.WriteLine(Ex.Message);
            }
            //Entrada en formato DD/MM/AAAA
            if (EncuentraCaracter(Fecha, Car1) > 1)
                Car = Car1[0];
            else
                Car = Car2[0];
             
            string[] splitArray = Fecha.Split(Car);
            Mes = Convert.ToInt16(splitArray[1]);
            switch (Mes)
            {
                case 1:
                    MesLetra = "Enero";
                    break;
                case 2:
                    MesLetra = "Febrero";
                    break;
                case 3:
                    MesLetra = "Marzo";
                    break;
                case 4:
                    MesLetra = "Abril";
                    break;
                case 5:
                    MesLetra = "Mayo";
                    break;
                case 6:
                    MesLetra = "Junio";
                    break;
                case 7:
                    MesLetra = "Julio";
                    break;
                case 8:
                    MesLetra = "Agosto";
                    break;
                case 9:
                    MesLetra = "Septiembre";
                    break;
                case 10:
                    MesLetra = "Octubre";
                    break;
                case 11:
                    MesLetra = "Noviembre";
                    break;
                case 12:
                    MesLetra = "Diciembre";
                    break;
                default:
                    break;
            }
            ValorDev = splitArray[0] + " de " + MesLetra + " de " + splitArray[2];

            ValorDev2 = ValorDev;

            return ValorDev2.ToUpper();
        }      


        public string Decimales(string Cadena)
        {
            double Cantidad = 0;
            // Redondea decimales cadena de entrada y devuelve su equivalente en Fix, reparando errores
            try
            {
                Cantidad = Convert.ToDouble(Cadena.Trim());
            }
            catch (Exception Ex)
            {
                Console.WriteLine("Error en Miscelaneas: " + Ex.Message);
                Cantidad = 0;
            }
            return String.Format("{0:N}", Cantidad);
        }

        public string Decimales(double Cadena)
        {
            double Cantidad = 0;
            // Redondea decimales cadena de entrada y devuelve su equivalente en Fix, reparando errores
            try
            {
                Cantidad = Cadena;
            }
            catch (Exception Ex)
            {
                Console.WriteLine("Error en Miscelanea: " + Ex.Message);
                Cantidad = 0;
            }
            return String.Format("{0:N}", Cantidad);
        }


        public void OcultarTab(string Cadena, TabControl Tbc, TabControl y)
        {   // Oculta el tabpage especificado por medio de TAG
            for (int Contx = 0; Contx < Tbc.TabPages.Count; Contx++)
            {
                if (Convert.ToString(Tbc.TabPages[Contx].Tag) == Cadena)
                {                    
                    TabPage x = new TabPage();
                    x = Tbc.TabPages[Contx];
                    Tbc.TabPages.Remove(x);
                    x.Parent = y;
                    Contx = -1;
                }
            }
        }


        public void MostrarTab(string Cadena, TabControl Tbc, TabControl y)
        {   // Muestra el tabpage especificado en TAG
            foreach (Control Ctr in y.Controls)
            {
                if (Ctr.GetType().Name == "TabPage")
                {                    
                    TabPage x = new TabPage();
                    x = (TabPage)Ctr;
                    if (Convert.ToString(x.Tag) == Cadena)
                    {
                        x.Parent = Tbc;
                    }
                }                
            }            
        }

               
        
        // Guardar preferencias de aplicacion en el registro
        public void SaveSetting(string appName, string section, string key, string setting)
        {
            // Los datos se guardan en:
            // HKEY_CURRENT_USER\Software\VB and VBA Program Settings
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Software\JJMDP\" + appName + "\\" + section);
            rk.SetValue(key, setting);
        }

        // Obtener preferencias de aplicaci�n del registro
        public string GetSetting(string appName, string section, string key)
        {
            return GetSetting(appName, section, key, "");
        }

        public string GetSetting(string appName, string section, string key, string sDefault)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\JJMDP\" + appName + "\\" + section);
            string s = sDefault;
            if (rk != null)
                s = (string)rk.GetValue(key);

            return s;
        }


     

    }


    public class CantidadLetra
    {
        private string[] sUnidades = {"", "un", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez", 
									"once", "doce", "trece", "catorce", "quince", "dieciseis", "diecisiete", "dieciocho", "diecinueve", "veinte", 
									"veinti�n", "veintidos", "veintitres", "veinticuatro", "veinticinco", "veintiseis", "veintisiete", "veintiocho", "veintinueve"};
        private string[] sDecenas  = { "", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" };
        private string[] sCentenas = { "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" };


        private string sResultado = "";


        public string ConvertirCadena(string sNumero)
        {
            double dNumero;
            double dNumAux = 0;
            char x;
            string sAux;
            bool Adicional = true;

            sResultado = " ";
            try
            {
                dNumero = Convert.ToDouble(sNumero);
            }
            catch
            {
                return "";
            }

            if (dNumero > 999999999999)
                return "";

            if (dNumero > 999999999)
            {
                dNumAux = dNumero % 1000000000000;
                sResultado += Numeros(dNumAux, 1000000000) + " mil ";
            }

            if (dNumero > 999999)
            {
                dNumAux = dNumero % 1000000000;
                sResultado += Numeros(dNumAux, 1000000) + " millones ";
            }

            if (dNumero > 999)
            {
                dNumAux = dNumero % 1000000;
                sResultado += Numeros(dNumAux, 1000) + " mil ";
            }

            dNumAux = dNumero % 1000;
            sResultado += Numeros(dNumAux, 1);


            //Enseguida verificamos si contiene punto, si es as�, los convertimos a texto.
            sAux = dNumero.ToString();

            if (sAux.IndexOf(".") >= 0)
            {
                sResultado += ObtenerDecimales(sAux);
                Adicional = false;
            }

            //Las siguientes l�neas convierten el primer caracter a may�scula.
            sAux = sResultado;
            x = char.ToUpper(sResultado[1]);
            sResultado = x.ToString();

            for (int i = 2; i < sAux.Length; i++)
                sResultado += sAux[i].ToString();

            if (Adicional == true)
            {
                sResultado += " pesos 00/100 MN";
            }

            return sResultado;
        }


        public string ConvertirCadena(double dNumero)
        {
            double dNumAux = 0;
            char x;
            string sAux;
            bool Adicional = true;

            sResultado = " ";

            if (dNumero > 999999999999)
                return "";

            if (dNumero > 999999999)
            {
                dNumAux = dNumero % 1000000000000;
                sResultado += Numeros(dNumAux, 1000000000) + " mil ";
            }

            if (dNumero > 999999)
            {
                dNumAux = dNumero % 1000000000;
                sResultado += Numeros(dNumAux, 1000000) + " millones ";
            }

            if (dNumero > 999)
            {
                dNumAux = dNumero % 1000000;
                sResultado += Numeros(dNumAux, 1000) + " mil ";
            }

            dNumAux = dNumero % 1000;
            sResultado += Numeros(dNumAux, 1);


            //Enseguida verificamos si contiene punto, si es as�, los convertimos a texto.
            sAux = dNumero.ToString();

            if (sAux.IndexOf(".") >= 0)
            {
                sResultado += ObtenerDecimales(sAux);
                Adicional = false;
            }

            //Las siguientes l�neas convierten el primer caracter a may�scula.
            sAux = sResultado;
            x = char.ToUpper(sResultado[1]);
            sResultado = x.ToString();

            for (int i = 2; i < sAux.Length; i++)
                sResultado += sAux[i].ToString();

            if (Adicional == true)
            {
                sResultado += " pesos 00/100 MN";
            }


            return sResultado;
        }


        private string Numeros(double dNumAux, double dFactor)
        {
            double dCociente = dNumAux / dFactor;
            double dNumero = 0;
            int iNumero = 0;
            string sNumero = "";
            string sTexto = "";

            if (dCociente >= 100)
            {
                dNumero = dCociente / 100;
                sNumero = dNumero.ToString();
                iNumero = int.Parse(sNumero[0].ToString());
                sTexto += this.sCentenas[iNumero] + " ";
            }

            dCociente = dCociente % 100;
            if (dCociente >= 30)
            {
                dNumero = dCociente / 10;
                sNumero = dNumero.ToString();
                iNumero = int.Parse(sNumero[0].ToString());
                if (iNumero > 0)
                    sTexto += this.sDecenas[iNumero] + " ";

                dNumero = dCociente % 10;
                sNumero = dNumero.ToString();
                iNumero = int.Parse(sNumero[0].ToString());
                if (iNumero > 0)
                    sTexto += "y " + this.sUnidades[iNumero] + " ";
            }

            else
            {
                dNumero = dCociente;
                sNumero = dNumero.ToString();
                if (sNumero.Length > 1)
                    if (sNumero[1] != '.')
                        iNumero = int.Parse(sNumero[0].ToString() + sNumero[1].ToString());
                    else
                        iNumero = int.Parse(sNumero[0].ToString());
                else
                    iNumero = int.Parse(sNumero[0].ToString());
                sTexto += this.sUnidades[iNumero] + " ";
            }

            return sTexto;
        }


        private string ObtenerDecimales(string sNumero)
        {
            string[] sNumPuntos;
            string sTexto = "";
            double dNumero = 0;

            sNumPuntos = sNumero.Split('.');


            dNumero = Convert.ToDouble(sNumPuntos[1]);
            sTexto = " pesos " + sNumPuntos[1] + ((sNumPuntos[1].Length > 1) ? "" : "0") + "/100 MN";  //Numeros(dNumero, 1);

            return sTexto;
        }
    }


}
