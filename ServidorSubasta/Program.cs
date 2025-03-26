using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class ServidorSubasta
{
    private static TcpListener servidor;
    private static List<TcpClient> clientes = new List<TcpClient>();
    private static Dictionary<string, int> pujas = new Dictionary<string, int>();
    private static List<Dictionary<string, int>> historialPujas = new List<Dictionary<string, int>>();
    private static int maxClientes = 2;
    private static object lockObj = new object();
    private static bool subastaFinalizada = false;

    static void Main()
    {
        servidor = new TcpListener(IPAddress.Any, 5000);
        servidor.Start();
        Console.WriteLine("Servidor iniciado en el puerto 5000. Esperando clientes...");

        while (clientes.Count < maxClientes)
        {
            TcpClient cliente = servidor.AcceptTcpClient();
            lock (lockObj)
            {
                if (clientes.Count < maxClientes)
                {
                    clientes.Add(cliente);
                    Thread clienteThread = new Thread(ManejarCliente);
                    clienteThread.Start(cliente);
                }
                else
                {
                    cliente.Close();
                }
            }
        }
    }

static void ManejarCliente(object obj)
{
    TcpClient cliente = (TcpClient)obj;
    NetworkStream stream = cliente.GetStream();
    byte[] buffer = new byte[1024];
    string nombre = "";

    try
    {
        while (true)
        {
            // Recibir el nombre del cliente
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                ClienteDesconectado(cliente);
                return;
            }

            nombre = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            lock (lockObj)
            {
                if (!pujas.ContainsKey(nombre))
                {
                    pujas[nombre] = 0;
                    Console.WriteLine($"{nombre} se ha inscrito en la subasta.");
                    EnviarMensaje(stream, "Inscripción exitosa");
                    break;
                }
                else
                {
                    EnviarMensaje(stream, "Error: Nombre ya registrado");
                }
            }
        }

        // Empezar las rondas de pujas
        while (!subastaFinalizada)
        {
            string mensaje = RecibirMensaje(stream);

            if (string.IsNullOrEmpty(mensaje))
            {
                ClienteDesconectado(cliente);
                return;
            }

            Console.WriteLine($"Mensaje recibido de {nombre}: '{mensaje}'");

            switch (mensaje.Trim())
            {
                case "1":
                    EnviarMensaje(stream, "Introduce tu oferta");
                    string ofertaTexto = RecibirMensaje(stream);
                    if (int.TryParse(ofertaTexto, out int cantidad))
                    {
                        lock (lockObj)
                        {
                            pujas[nombre] = cantidad;
                            Console.WriteLine($"{nombre} ha pujado {cantidad}");
                        }
                        EnviarMensaje(stream, "Puja recibida");
                        GuardarHistorial();
                    }
                    else
                    {
                        EnviarMensaje(stream, "Error: Introduce un número válido");
                    }
                    break;

                case "2":
                    string resultado = "Pujas de la última ronda:\n";
                    lock (lockObj)
                    {
                        foreach (var p in pujas)
                            resultado += $"{p.Key}: {p.Value}\n";
                    }
                    EnviarMensaje(stream, resultado);
                    break;

                case "3":
                    string historial = "Historial de pujas:\n";
                    lock (lockObj)
                    {
                        foreach (var ronda in historialPujas)
                        {
                            foreach (var p in ronda)
                            {
                                historial += $"{p.Key}: {p.Value}\n";
                            }
                            historial += "----\n";
                        }
                    }
                    EnviarMensaje(stream, historial);
                    break;

                case "4":
                    string maxPujaMsg = "No hay pujas aún";
                    lock (lockObj)
                    {
                        if (pujas.Count > 0)
                        {
                            var maxPuja = pujas.Aggregate((l, r) => l.Value > r.Value ? l : r);
                            maxPujaMsg = $"Mayor puja: {maxPuja.Key} con {maxPuja.Value}";
                        }
                    }
                    EnviarMensaje(stream, maxPujaMsg);
                    break;

                case "5":
                    Console.WriteLine($"{nombre} ha salido de la subasta.");
                    ClienteDesconectado(cliente);
                    return;

                default:
                    EnviarMensaje(stream, "Opción no válida, intenta de nuevo.");
                    break;
            }
        }
    }
    catch (Exception)
    {
        ClienteDesconectado(cliente);
    }
}

static void ClienteDesconectado(TcpClient cliente)
{
    lock (lockObj)
    {
        if (!clientes.Contains(cliente)) return;
        clientes.Remove(cliente);
    }

    Console.WriteLine("Un cliente se ha desconectado. Finalizando la subasta...");

    FinalizarSubasta();
}

static void FinalizarSubasta()
{
    lock (lockObj)
    {
        if (subastaFinalizada) return;
        subastaFinalizada = true;
    }

    Console.WriteLine("Subasta finalizada. Cerrando conexiones...");

    foreach (var cliente in clientes)
    {
        try
        {
            if (cliente.Connected)
            {
                NetworkStream stream = cliente.GetStream();
                EnviarMensaje(stream, "Puja finalizada");
            }
            cliente.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cerrar conexión: {ex.Message}");
        }
    }

    clientes.Clear();
    servidor.Stop();
    Environment.Exit(0);
}


    static void EnviarMensaje(NetworkStream stream, string mensaje)
    {
        byte[] datos = Encoding.UTF8.GetBytes(mensaje);
        stream.Write(datos, 0, datos.Length);
        stream.Flush();
    }

    static string RecibirMensaje(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesLeidos = stream.Read(buffer, 0, buffer.Length);
        if (bytesLeidos == 0) return "";

        return Encoding.UTF8.GetString(buffer, 0, bytesLeidos).Trim();
    }

    static void GuardarHistorial()
    {
        lock (lockObj)
        {
            historialPujas.Add(new Dictionary<string, int>(pujas));
            Console.WriteLine("Ronda finalizada. Se guardó el historial de pujas.");
        }
    }
}
