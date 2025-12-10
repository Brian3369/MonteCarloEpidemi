using System;
using System.Diagnostics;
using System.IO;

namespace EpidemiaSecuencial
{
    class Program
    {
        // Parámetros del modelo
        const int SIZE = 1000;
        const int DAYS = 365;
        const double PROB_CONTAGIO = 0.25; // Probabilidad de contagio por vecino infectado
        const double PROB_RECUPERACION = 0.1; // Probabilidad de recuperarse
        const double PROB_MUERTE = 0.01; // Probabilidad de morir (opcional, parte de R)
        
        // Estados
        const byte SUSCEPTIBLE = 0;
        const byte INFECTADO = 1;
        const byte RECUPERADO = 2; // Incluye muertos para simplificar SIR

        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Simulación Secuencial SIR...");
            
            // Inicialización
            byte[,] grid = new byte[SIZE, SIZE];
            byte[,] nextGrid = new byte[SIZE, SIZE];
            Random rand = new Random(42); // Semilla fija para reproducibilidad

            // Población inicial
            int initialInfected = 0;
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    if (rand.NextDouble() < 0.0001) // 0.01% infectados iniciales
                    {
                        grid[i, j] = INFECTADO;
                        initialInfected++;
                    }
                    else
                    {
                        grid[i, j] = SUSCEPTIBLE;
                    }
                }
            }

            Console.WriteLine($"Población: {SIZE * SIZE}, Infectados iniciales: {initialInfected}");

            // Asegurar que existe la carpeta de datos
            Directory.CreateDirectory("datos");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Archivo para estadísticas
            using (StreamWriter writer = new StreamWriter("datos/Resultados_Secuencial.csv"))
            {
                writer.WriteLine("Dia,Susceptibles,Infectados,Recuperados");

                for (int day = 0; day < DAYS; day++)
                {
                    int s = 0, i_count = 0, r = 0;

                    // Actualización de la grilla
                    for (int x = 0; x < SIZE; x++)
                    {
                        for (int y = 0; y < SIZE; y++)
                        {
                            byte estado = grid[x, y];
                            byte nuevoEstado = estado;

                            if (estado == INFECTADO)
                            {
                                double p = rand.NextDouble();
                                if (p < PROB_RECUPERACION)
                                {
                                    nuevoEstado = RECUPERADO;
                                }
                                // Podríamos agregar muerte aquí si fuera un estado separado
                            }
                            else if (estado == SUSCEPTIBLE)
                            {
                                // Chequear vecinos (8 vecinos)
                                int vecinosInfectados = ContarVecinosInfectados(grid, x, y);
                                if (vecinosInfectados > 0)
                                {
                                    // Probabilidad de NO infectarse es (1 - p)^k
                                    // Probabilidad de infectarse es 1 - (1 - p)^k
                                    double probInfeccionTotal = 1.0 - Math.Pow(1.0 - PROB_CONTAGIO, vecinosInfectados);
                                    if (rand.NextDouble() < probInfeccionTotal)
                                    {
                                        nuevoEstado = INFECTADO;
                                    }
                                }
                            }

                            nextGrid[x, y] = nuevoEstado;

                            // Estadísticas
                            if (nuevoEstado == SUSCEPTIBLE) s++;
                            else if (nuevoEstado == INFECTADO) i_count++;
                            else r++;
                        }
                    }

                    // Swap grids
                    byte[,] temp = grid;
                    grid = nextGrid;
                    nextGrid = temp;

                    writer.WriteLine($"{day},{s},{i_count},{r}");
                    if (day % 10 == 0) Console.WriteLine($"Día {day}: S={s}, I={i_count}, R={r}");
                    
                    // Si ya no hay infectados, podemos parar (opcional)
                    if (i_count == 0) break;
                }
            }

            sw.Stop();
            Console.WriteLine($"Simulación terminada en {sw.ElapsedMilliseconds} ms");

            // Guardar tiempo de ejecución
            try 
            {
                File.AppendAllText("datos/tiempos.csv", $"Secuencial,1,{sw.ElapsedMilliseconds}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error escribiendo tiempos: {ex.Message}");
            }
        }

        static int ContarVecinosInfectados(byte[,] grid, int x, int y)
        {
            int count = 0;
            // Recorrer vecinos 3x3
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    int nx = x + i;
                    int ny = y + j;

                    // Chequeo de bordes
                    if (nx >= 0 && nx < SIZE && ny >= 0 && ny < SIZE)
                    {
                        if (grid[nx, ny] == INFECTADO)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }
    }
}
