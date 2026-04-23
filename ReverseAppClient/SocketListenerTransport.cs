using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ReverseAppClient
{
    internal sealed class SocketListenerTransport : IDisposable
    {
        private readonly Func<Socket, StringBuilder, bool> _onBufferReceived;
        private readonly Action<Socket, Exception> _onClientError;
        private readonly Action<Socket> _onClientDisconnected;
        private readonly Action<string> _onStatusChanged;
        private readonly Action<string> _onLog;
        private readonly object _sync = new object();
        private readonly List<Socket> _activeSockets = new List<Socket>();

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _stopping;

        public SocketListenerTransport(
            Func<Socket, StringBuilder, bool> onBufferReceived,
            Action<Socket, Exception> onClientError,
            Action<Socket> onClientDisconnected,
            Action<string> onStatusChanged,
            Action<string> onLog)
        {
            _onBufferReceived = onBufferReceived;
            _onClientError = onClientError;
            _onClientDisconnected = onClientDisconnected;
            _onStatusChanged = onStatusChanged;
            _onLog = onLog;
        }

        public void Start(int port)
        {
            if (_acceptThread != null)
            {
                return;
            }

            _stopping = false;
            _acceptThread = new Thread(new ThreadStart(delegate { AcceptLoop(port); }));
            _acceptThread.IsBackground = true;
            _acceptThread.Start();
        }

        public void Stop()
        {
            _stopping = true;

            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }
            }
            catch
            {
            }

            lock (_sync)
            {
                foreach (Socket socket in _activeSockets)
                {
                    CloseSocket(socket);
                }
                _activeSockets.Clear();
            }
        }

        private void AcceptLoop(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _onStatusChanged?.Invoke(@"Escuchando puerto " + port + "...");

                while (!_stopping)
                {
                    Socket socketEx;
                    try
                    {
                        socketEx = _listener.AcceptSocket();
                    }
                    catch (SocketException)
                    {
                        if (_stopping)
                        {
                            break;
                        }

                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    if (_stopping)
                    {
                        CloseSocket(socketEx);
                        break;
                    }

                    IPEndPoint ipend = (IPEndPoint)socketEx.RemoteEndPoint;
                    _onStatusChanged?.Invoke(@"Conexion de " + IPAddress.Parse(ipend.Address.ToString()));

                    lock (_sync)
                    {
                        _activeSockets.Add(socketEx);
                    }

                    var worker = new Thread(new ThreadStart(delegate { ClientLoop(socketEx); }));
                    worker.IsBackground = true;
                    worker.Start();
                }
            }
            catch (Exception ex)
            {
                if (!_stopping)
                {
                    _onLog?.Invoke("<Error servidor: " + ex.Message + ">\r\n");
                }
            }
        }

        private void ClientLoop(Socket socketNuevo)
        {
            NetworkStream networkStream = null;
            StreamWriter streamWriter = null;
            StreamReader streamReader = null;
            var buffer = new StringBuilder();

            try
            {
                networkStream = new NetworkStream(socketNuevo);
                streamReader = new StreamReader(networkStream);
                streamWriter = new StreamWriter(networkStream);

                while (!_stopping)
                {
                    try
                    {
                        var line = streamReader.ReadLine();
                        if (line == null)
                        {
                            throw new IOException("Conexion cerrada");
                        }

                        buffer.Append(line);
                        buffer.Append("\r\n");
                        var keepAlive = _onBufferReceived == null ? true : _onBufferReceived(socketNuevo, buffer);
                        buffer.Length = 0;
                        if (!keepAlive)
                        {
                            break;
                        }
                    }
                    catch (Exception err)
                    {
                        _onClientError?.Invoke(socketNuevo, err);
                        break;
                    }
                }
            }
            finally
            {
                try
                {
                    if (streamReader != null)
                    {
                        streamReader.Close();
                    }
                }
                catch
                {
                }

                try
                {
                    if (streamWriter != null)
                    {
                        streamWriter.Close();
                    }
                }
                catch
                {
                }

                try
                {
                    if (networkStream != null)
                    {
                        networkStream.Close();
                    }
                }
                catch
                {
                }

                CloseSocket(socketNuevo);

                lock (_sync)
                {
                    _activeSockets.Remove(socketNuevo);
                }

                _onClientDisconnected?.Invoke(socketNuevo);
            }
        }

        private static void CloseSocket(Socket socket)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
