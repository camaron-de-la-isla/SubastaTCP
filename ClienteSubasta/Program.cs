using System;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        try
        {
            using TcpClient cliente = new TcpClient("127.0.0.1", 5000);
            using NetworkStream stream = cliente.GetStream();

            Console.Write("Introduce tu nombre: ");
            string nombre;
            string respuesta;

            do
            {
                nombre = Console.ReadLine();
                EnviarMensaje(stream, nombre);
                respuesta = RecibirMensaje(stream);
                Console.WriteLine(respuesta);

                if (respuesta.Contains("Error: Nombre ya registrado"))
                {
                    Console.Write("Introduce un nombre diferente: ");
                }

            } while (respuesta.Contains("Error: Nombre ya registrado"));

            while (true)
            {
                Console.WriteLine("\nMenú:");
                Console.WriteLine("1. Pujar");
                Console.WriteLine("2. Mostrar resultado ronda");
                Console.WriteLine("3. Ver histórico de pujas");
                Console.WriteLine("4. Ver mayor puja");
                Console.WriteLine("5. Salir");

                string opcion;
                do
                {
                    Console.Write("Selecciona una opción: ");
                    opcion = Console.ReadLine();

                    if (!int.TryParse(opcion, out int numero) || numero < 1 || numero > 5)
                    {
                        Console.WriteLine("Opción no válida. Debe ser un número entre 1 y 5.");
                    }
                    else
                    {
                        break; // Opción válida
                    }

                } while (true);

                EnviarMensaje(stream, opcion);

                string resultado = RecibirMensaje(stream);
                Console.WriteLine(resultado);

                if (resultado.StartsWith("La subasta ha finalizado"))
                {
                    break; // Salir del bucle y cerrar la conexión
                }

                if (opcion == "5") break;

                if (opcion == "1")
                {
                    if (resultado.Equals("Introduce tu oferta"))
                    {
                        string oferta;
                        do
                        {
                            Console.Write("(número entero): ");
                            oferta = Console.ReadLine();

                            if (!int.TryParse(oferta, out _))
                            {
                                Console.WriteLine("Debes ingresar un número válido.");
                            }
                            else
                            {
                                break;
                            }

                        } while (true);

                        EnviarMensaje(stream, oferta);
                        resultado = RecibirMensaje(stream);
                        Console.WriteLine(resultado);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    static void EnviarMensaje(NetworkStream stream, string mensaje)
    {
        byte[] datos = Encoding.UTF8.GetBytes(mensaje);
        stream.Write(datos, 0, datos.Length);
    }

    static string RecibirMensaje(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesLeidos = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesLeidos);
    }
}