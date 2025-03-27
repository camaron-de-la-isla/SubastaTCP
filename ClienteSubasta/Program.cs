using System;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        try
        {
            // Establece la conexión con el servidor en la dirección y puerto especificados
            using TcpClient cliente = new TcpClient("127.0.0.1", 5000);
            using NetworkStream stream = cliente.GetStream();

            Console.Write("Introduce tu nombre: ");
            string nombre;
            string respuesta;

            // Solicita el nombre del usuario y verifica si ya está en uso
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

            // Bucle principal para interactuar con la subasta
            while (true)
            {
                // Muestra el menú de opciones disponibles
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

                    // Valida que la opción ingresada sea un número entre 1 y 5
                    if (!int.TryParse(opcion, out int numero) || numero < 1 || numero > 5)
                    {
                        Console.WriteLine("Opción no válida. Debe ser un número entre 1 y 5.");
                    }
                    else
                    {
                        break; // Opción válida
                    }

                } while (true);

                // Envía la opción seleccionada al servidor
                EnviarMensaje(stream, opcion);

                // Recibe la respuesta del servidor y la muestra en pantalla
                string resultado = RecibirMensaje(stream);
                Console.WriteLine(resultado);

                // Si la subasta ha finalizado, salir del bucle
                if (resultado.StartsWith("La subasta ha finalizado"))
                {
                    break;
                }

                // Si el usuario elige salir (opción 5), confirmar la salida con el servidor
                if (opcion == "5")
                {
                    EnviarMensaje(stream, opcion);
                    resultado = RecibirMensaje(stream);
                    Console.WriteLine(resultado);
                    if (resultado.StartsWith("La subasta ha finalizado"))
                    {
                        break;
                    }
                    continue;
                }

                // Si el usuario elige pujar (opción 1)
                if (opcion == "1")
                {
                    if (resultado.Equals("Introduce tu oferta"))
                    {
                        string oferta;
                        do
                        {
                            Console.Write("(número entero): ");
                            oferta = Console.ReadLine();

                            // Verifica que la oferta ingresada sea un número válido
                            if (!int.TryParse(oferta, out _))
                            {
                                Console.WriteLine("Debes ingresar un número válido.");
                            }
                            else
                            {
                                break;
                            }

                        } while (true);

                        // Envía la oferta al servidor y muestra la respuesta
                        EnviarMensaje(stream, oferta);
                        resultado = RecibirMensaje(stream);
                        Console.WriteLine(resultado);
                    }
                }
            }
        }
        catch (Exception e)
        {
            // Captura y muestra cualquier error que ocurra durante la ejecución
            Console.WriteLine("Error: " + e.Message);
        }
    }

    // Envía un mensaje al servidor a través del flujo de red
    static void EnviarMensaje(NetworkStream stream, string mensaje)
    {
        byte[] datos = Encoding.UTF8.GetBytes(mensaje);
        stream.Write(datos, 0, datos.Length);
    }

    // Recibe un mensaje del servidor y lo devuelve como una cadena
    static string RecibirMensaje(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesLeidos = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesLeidos);
    }
}
