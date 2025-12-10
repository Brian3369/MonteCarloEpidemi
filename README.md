# Simulación Monte-Carlo de Epidemias Paralela (Modelo SIR)

## 1. Introducción
Este proyecto implementa una simulación estocástica de la propagación de una epidemia utilizando el modelo **SIR (Susceptible-Infectado-Recuperado)** sobre una grilla bidimensional. El objetivo principal es comparar el rendimiento de una implementación secuencial frente a una implementación paralela utilizando **Domain Decomposition** en C#, analizando el *Speed-up* obtenido al aumentar el número de núcleos de procesamiento.

## 2. Modelo Matemático

### 2.1. Representación del Espacio
La población se modela en una grilla cuadrada de $N \times N$ celdas (donde $N=1000$, totalizando $1,000,000$ de individuos). Cada celda $(i, j)$ representa a un individuo que puede estar en uno de tres estados:
*   **Susceptible ($S$)**: Sano pero puede contraer la enfermedad.
*   **Infectado ($I$)**: Portador de la enfermedad y contagioso.
*   **Recuperado ($R$)**: Inmune o fallecido (ya no participa en la dinámica de contagio).

### 2.2. Dinámica de Transición
La simulación avanza en pasos de tiempo discretos ($t = 0, 1, ..., 365$ días). En cada paso, se aplican las siguientes reglas probabilísticas:

#### A. Contagio ($S \to I$)
Un individuo susceptible en la posición $(x, y)$ interactúa con sus 8 vecinos (Moore neighborhood). Sea $k$ el número de vecinos infectados alrededor de $(x, y)$.
La probabilidad de **NO** ser infectado por un solo vecino infectado es $(1 - \beta)$, donde $\beta$ es la probabilidad de transmisión por contacto ($P_{contagio}$).
Por lo tanto, la probabilidad de **NO** ser infectado por ninguno de los $k$ vecinos es:
$$ P(\text{no infección}) = (1 - \beta)^k $$
La probabilidad de infectarse en este paso de tiempo es el complemento:
$$ P(S \to I) = 1 - (1 - \beta)^k $$

En esta simulación, $\beta = 0.25$.

#### B. Recuperación ($I \to R$)
Un individuo infectado tiene una probabilidad constante $\gamma$ ($P_{recuperacion}$) de recuperarse en cada paso de tiempo, independientemente de sus vecinos.
$$ P(I \to R) = \gamma $$

En esta simulación, $\gamma = 0.1$.

## 3. Implementación Computacional

### 3.1. Algoritmo Secuencial
El algoritmo recorre la grilla celda por celda. Para evitar sesgos direccionales y condiciones de carrera lógicas, se utiliza un esquema de **doble buffer** (`grid` actual y `nextGrid`).
1.  Se lee el estado de $(x, y)$ y sus vecinos desde `grid`.
2.  Se calcula el nuevo estado y se escribe en `nextGrid`.
3.  Al finalizar el barrido completo, se intercambian los punteros de las matrices.

### 3.2. Algoritmo Paralelo
Se utiliza **Descomposición de Dominio** (Domain Decomposition) para paralelizar la actualización de la grilla.
*   **Particionamiento**: La grilla se divide en bandas horizontales. Cada hilo es responsable de actualizar un rango de filas $[start, end)$.
*   **Sincronización**: Se utiliza `Parallel.ForEach` con un `Partitioner` para equilibrar la carga.
*   **Aleatoriedad**: Se utiliza `ThreadLocal<Random>` para garantizar que cada hilo tenga su propio generador de números aleatorios, evitando bloqueos y garantizando thread-safety.
*   **Reducción**: Cada hilo calcula estadísticas locales (conteo de S, I, R) que luego se suman atómicamente (`Interlocked.Add`) a las variables globales al final de cada iteración.

## 4. Análisis de Resultados

### 4.1. Curvas Epidemiológicas
La simulación produce las curvas clásicas del modelo SIR:
1.  **Fase Exponencial**: Rápido crecimiento de infectados debido a la alta disponibilidad de susceptibles.
2.  **Pico Epidémico**: Punto máximo de infectados simultáneos.
3.  **Inmunidad de Rebaño**: A medida que $S$ disminuye y $R$ aumenta, la probabilidad de encuentro entre un $S$ y un $I$ baja, extinguiendo la epidemia.

*(Ver animación `datos/Evolucion_SIR.gif` generada en el proyecto)*

### 4.2. Escalabilidad (Strong Scaling)
Se realizaron experimentos variando el número de hilos ($p = 1, 2, 4, 8$) manteniendo el tamaño del problema fijo ($1000 \times 1000$).

**Métricas:**
*   **Tiempo de Ejecución ($T_p$)**: Tiempo total para simular 365 días.
*   **Speed-up ($S_p$)**: $S_p = \frac{T_1}{T_p}$

**Resultados Observados (Ejemplo):**
*   **1 Hilo**: ~5300 ms (Base).
*   **2 Hilos**: ~2800 ms ($S_p \approx 1.9$). Casi lineal.
*   **4 Hilos**: ~1500 ms ($S_p \approx 3.5$). Excelente escalabilidad.
*   **8 Hilos**: ~950 ms ($S_p \approx 5.6$). Se observa cierta degradación debido a la Ley de Amdahl y el overhead de creación de tareas, pero sigue siendo muy eficiente.

*(Ver gráfica `datos/Speedup.png`)*

## 5. Instrucciones de Ejecución

### Requisitos
*   .NET 8.0 SDK
*   Python 3.x (con `pandas`, `matplotlib`, `pillow`)

### Pasos
1.  **Compilar**:
    ```bash
    dotnet build Secuencial/Secuencial.csproj
    dotnet build Paralelo/Paralelo.csproj
    ```

2.  **Ejecutar Simulaciones**:
    ```bash
    # Secuencial
    dotnet run --project Secuencial/Secuencial.csproj

    # Paralelo (Escalabilidad)
    dotnet run --project Paralelo/Paralelo.csproj 1
    dotnet run --project Paralelo/Paralelo.csproj 2
    dotnet run --project Paralelo/Paralelo.csproj 4
    dotnet run --project Paralelo/Paralelo.csproj 8
    ```

3.  **Generar Informe Visual**:
    ```bash
    python Visualizacion/analisis.py
    ```
    Los resultados se guardarán en la carpeta `datos/`.

## 6. Conclusión
La paralelización de la simulación Monte-Carlo mediante descomposición de dominio demostró ser altamente efectiva. La independencia de las actualizaciones locales (salvo por la lectura de vecinos, resuelta con doble buffer) permite una escalabilidad casi lineal en bajos números de núcleos. Este enfoque es vital para simulaciones epidemiológicas realistas que requieren millones de agentes y múltiples escenarios.