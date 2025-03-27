using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class ServidorSubasta
{
    // Declaración de variables estáticas para la gestión del servidor y la subasta.
    private static TcpListener servidor;
    private static List<TcpClient> clientes = new List<TcpClient>();
    private static Dictionary<string, int> pujas = new Dictionary<string, int>();
    private static List<Dictionary<string, int>> historialPujas = new List<Dictionary<string, int>>();
    private static int maxClientes = 2;
    private static object lockObj = new object();
    private static bool subastaFinalizada = false;
    private static string ganadorRonda = "";
    private static int clientesPujaron = 0;
    private static string ganadorHistorico = "";
    private static int pujaMaximaHistorica = 0; 

    private static HashSet<string> ganadoresHistoricos = new HashSet<string>(); // Almacena ganadores históricos
    private static HashSet<string> clientesPujaronRonda = new HashSet<string>(); // Para rastrear quién ha pujado en la ronda

    
    static void Main()
    {
        // Inicialización del servidor TCP en el puerto 5000.
        servidor = new TcpListener(IPAddress.Any, 5000);
        servidor.Start();
        Console.WriteLine("Servidor iniciado en el puerto 5000. Esperando clientes...");

        // Aceptación de conexiones de clientes hasta alcanzar el máximo permitido.
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
        // Manejo de la conexión de un cliente individual.
        TcpClient cliente = (TcpClient)obj;
        NetworkStream stream = cliente.GetStream();
        byte[] buffer = new byte[1024];
        string nombre = "";

        try
        {
            // Bucle para recibir el nombre del cliente.
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
                        // Manejo de la opción de pujar.
                        if (clientesPujaronRonda.Contains(nombre))
                        {
                            EnviarMensaje(stream, "Ya has pujado en esta ronda.");
                        }
                        else
                        {
                            EnviarMensaje(stream, "Introduce tu oferta");
                            string ofertaTexto = RecibirMensaje(stream);
                            if (int.TryParse(ofertaTexto, out int cantidad))
                            {
                                lock (lockObj)
                                {
                                    pujas[nombre] = cantidad;
                                    Console.WriteLine($"{nombre} ha pujado {cantidad}");
                                    clientesPujaron++;
                                    clientesPujaronRonda.Add(nombre);
                                    
                                    if (cantidad > pujaMaximaHistorica)
                                    {
                                        pujaMaximaHistorica = cantidad;
                                        ganadorHistorico = nombre;
                                        Console.WriteLine($"Nuevo ganador histórico: {nombre} con {cantidad}");
                                    }

                                    if (clientesPujaron == maxClientes)
                                    {
                                        DeterminarGanador();
                                        GuardarHistorial();
                                        clientesPujaron = 0;
                                        clientesPujaronRonda.Clear(); // Reiniciar para la próxima ronda
                                    }
                                }
                                EnviarMensaje(stream, "Puja recibida");
                            }
                            else
                            {
                                EnviarMensaje(stream, "Error: Introduce un número válido");
                            }
                        }
                        break;

                    case "2":
                        // Manejo de la opción de mostrar resultados de la ronda.
                        string resultado = "Pujas de la última ronda:\n";
                        lock (lockObj)
                        {
                            foreach (var p in pujas)
                                resultado += $"{p.Key}: {p.Value}\n";

                            // Determinar y añadir el ganador al resultado
                            if (pujas.Count > 0)
                            {
                                var ganador = pujas.Aggregate((l, r) => l.Value > r.Value ? l : r);
                                resultado += $"Ganador de la ronda: {ganador.Key} con {ganador.Value}\n";
                            }
                            else
                            {
                                resultado += "Aún no hay pujas en esta ronda.\n";
                            }
                        }
                        EnviarMensaje(stream, resultado);
                        break;

                    case "3":
                        // Manejo de la opción de mostrar el historial de pujas.
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
                        // Manejo de la opción de mostrar la puja máxima.
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
                        // Manejo de la opción de salir de la subasta\.
                        lock (lockObj)
                        {
                            if (nombre == ganadorHistorico)
                            {
                                EnviarMensaje(stream, "No puedes salir, eres el ganador histórico.");
                            }
                            else
                            {
                                Console.WriteLine($"{nombre} ha salido de la subasta.");
                                EnviarMensaje(stream, $"La subasta ha finalizado. Ganador: {DeterminarGanador()}");
                                ClienteDesconectado(cliente);
                                return;
                            }
                        }
                        break;
                    
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
        // Manejo de la desconexión de un cliente.
        lock (lockObj)
        {
            if (!clientes.Contains(cliente)) return;
            clientes.Remove(cliente);
        }

        Console.WriteLine("Un cliente se ha desconectado.");

        // Si solo queda un cliente, enviarle el ganador y finalizar.
        if (clientes.Count == 1)
        {
            try
            {
                NetworkStream stream = clientes[0].GetStream();
                EnviarMensaje(stream, $"La subasta ha finalizado. Ganador: {DeterminarGanador()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar mensaje al cliente restante: {ex.Message}");
            }
        }
        else if (clientes.Count == 0)
        {
            FinalizarSubasta();
        }
    }

    static void FinalizarSubasta()
    // Manejo de la finalización de la subasta.
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
                // Enviar mensaje de finalización a cada cliente.
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

        // Guardar el historial de pujas y cerrar el servidor.
        clientes.Clear();
        servidor.Stop();
        Environment.Exit(0);
    }

    static void EnviarMensaje(NetworkStream stream, string mensaje)
    {
        // Enviar un mensaje al cliente.
        byte[] datos = Encoding.UTF8.GetBytes(mensaje);
        stream.Write(datos, 0, datos.Length);
        stream.Flush();
    }

    static string RecibirMensaje(NetworkStream stream)
    {
        // Recibir un mensaje del cliente.
        byte[] buffer = new byte[1024];
        int bytesLeidos = stream.Read(buffer, 0, buffer.Length);
        if (bytesLeidos == 0) return "";

        return Encoding.UTF8.GetString(buffer, 0, bytesLeidos).Trim();
    }

    static void GuardarHistorial()
    {
        // Guardar el historial de pujas.
        lock (lockObj)
        {
            historialPujas.Add(new Dictionary<string, int>(pujas));
        }
    }

    static string DeterminarGanador()
    {
        lock (lockObj)
        {
            // Determinar el ganador de la ronda.
            if (pujas.Count > 0)
            {
                var ganador = pujas.Aggregate((l, r) => l.Value > r.Value ? l : r);
                ganadorRonda = ganador.Key;
                return $"{ganador.Key} con {ganador.Value}";
            }
            else
            {
                ganadorRonda = "";
                return "No hubo pujas";
            }
        }
    }
}