using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace EpidemiaParalela
{
    class Program
    {
        // Parámetros del modelo
        const int SIZE = 1000;
        const int DAYS = 365;
        const double PROB_CONTAGIO = 0.25;
        const double PROB_RECUPERACION = 0.1;
        
        const byte SUSCEPTIBLE = 0;
        const byte INFECTADO = 1;
        const byte RECUPERADO = 2;

        // ThreadLocal para generación de números aleatorios en paralelo
        static ThreadLocal<Random> threadRand = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        static void Main(string[] args)
        {
            // Configuración de paralelismo (por defecto usa todos los cores, pero se puede limitar para experimentos)
            int degreeOfParallelism = Environment.ProcessorCount;
            if (args.Length > 0 && int.TryParse(args[0], out int val))
            {
                degreeOfParallelism = val;
            }

            Console.WriteLine($"Iniciando Simulación Paralela SIR con {degreeOfParallelism} hilos...");

            byte[,] grid = new byte[SIZE, SIZE];
            byte[,] nextGrid = new byte[SIZE, SIZE];
            Random initRand = new Random(42);

            int initialInfected = 0;
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    if (initRand.NextDouble() < 0.0001)
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

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism };

            // Asegurar que existe la carpeta de datos
            Directory.CreateDirectory("datos");

            using (StreamWriter writer = new StreamWriter($"datos/Resultados_Paralelo_{degreeOfParallelism}.csv"))
            {
                writer.WriteLine("Dia,Susceptibles,Infectados,Recuperados");

                for (int day = 0; day < DAYS; day++)
                {
                    int totalS = 0;
                    int totalI = 0;
                    int totalR = 0;

                    // Particionamiento de la grilla (Domain Decomposition)
                    // Usamos un Partitioner para dividir el rango de filas en bloques
                    var rangePartitioner = Partitioner.Create(0, SIZE);

                    // Ejecución paralela de la actualización y reducción de estadísticas
                    Parallel.ForEach(rangePartitioner, parallelOptions, 
                        // Inicialización de variables locales del hilo
                        () => (S: 0, I: 0, R: 0),
                        // Cuerpo del bucle
                        (range, loopState, localStats) =>
                        {
                            Random rng = threadRand.Value!;
                            
                            // Iteramos sobre el bloque de filas asignado a este hilo
                            for (int x = range.Item1; x < range.Item2; x++)
                            {
                                for (int y = 0; y < SIZE; y++)
                                {
                                    byte estado = grid[x, y];
                                    byte nuevoEstado = estado;

                                    if (estado == INFECTADO)
                                    {
                                        if (rng.NextDouble() < PROB_RECUPERACION)
                                        {
                                            nuevoEstado = RECUPERADO;
                                        }
                                    }
                                    else if (estado == SUSCEPTIBLE)
                                    {
                                        int vecinosInfectados = ContarVecinosInfectados(grid, x, y);
                                        if (vecinosInfectados > 0)
                                        {
                                            double probInfeccionTotal = 1.0 - Math.Pow(1.0 - PROB_CONTAGIO, vecinosInfectados);
                                            if (rng.NextDouble() < probInfeccionTotal)
                                            {
                                                nuevoEstado = INFECTADO;
                                            }
                                        }
                                    }

                                    nextGrid[x, y] = nuevoEstado;

                                    // Conteo local
                                    if (nuevoEstado == SUSCEPTIBLE) localStats.S++;
                                    else if (nuevoEstado == INFECTADO) localStats.I++;
                                    else localStats.R++;
                                }
                            }
                            return localStats;
                        },
                        // Reducción final (thread-safe)
                        (finalStats) =>
                        {
                            Interlocked.Add(ref totalS, finalStats.S);
                            Interlocked.Add(ref totalI, finalStats.I);
                            Interlocked.Add(ref totalR, finalStats.R);
                        }
                    );

                    // Swap grids
                    byte[,] temp = grid;
                    grid = nextGrid;
                    nextGrid = temp;

                    writer.WriteLine($"{day},{totalS},{totalI},{totalR}");
                    
                    if (day % 10 == 0) Console.WriteLine($"Día {day}: S={totalS}, I={totalI}, R={totalR}");
                    if (totalI == 0) break;
                }
            }

            sw.Stop();
            Console.WriteLine($"Simulación Paralela terminada en {sw.ElapsedMilliseconds} ms");

            // Guardar tiempo de ejecución
            try
            {
                File.AppendAllText("datos/tiempos.csv", $"Paralelo,{degreeOfParallelism},{sw.ElapsedMilliseconds}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error escribiendo tiempos: {ex.Message}");
            }
        }

        static int ContarVecinosInfectados(byte[,] grid, int x, int y)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    int nx = x + i;
                    int ny = y + j;

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
