# Simulación Monte-Carlo de Epidemias (Modelo SIR)

Este proyecto implementa una simulación de propagación de epidemias utilizando el modelo SIR (Susceptible-Infectado-Recuperado) en una grilla 2D de 1000x1000 celdas.

El proyecto contiene dos implementaciones en C#:
1. **Secuencial**: Ejecución en un solo hilo.
2. **Paralela**: Ejecución utilizando `Parallel.ForEach` y descomposición de dominio, con reducción paralela de estadísticas.

## Estructura del Proyecto

- `Secuencial/`: Código fuente de la versión secuencial.
- `Paralelo/`: Código fuente de la versión paralela.
- `Visualizacion/`: Scripts de Python para generar gráficas y animaciones.
- `Resultados/`: Carpeta donde se guardarán los CSV de salida (debe crearse o el código la usará en la raíz).

## Requisitos

- .NET SDK 8.0 o superior.
- Python 3.x (para visualización) con `matplotlib` y `pandas`.

## Compilación

Desde la raíz del proyecto:

```bash
dotnet build Secuencial/Secuencial.csproj
dotnet build Paralelo/Paralelo.csproj
```

## Ejecución

### 1. Versión Secuencial

```bash
dotnet run --project Secuencial/Secuencial.csproj
```
Esto generará `Resultados_Secuencial.csv`.

### 2. Versión Paralela

La versión paralela acepta un argumento opcional para el número de hilos (cores).

```bash
dotnet run --project Paralelo/Paralelo.csproj 4
```
(Ejecuta con 4 hilos). Generará `Resultados_Paralelo_4.csv`.

Para realizar el experimento de escalabilidad (Strong Scaling), ejecute con 1, 2, 4 y 8 hilos:

```bash
dotnet run --project Paralelo/Paralelo.csproj 1
dotnet run --project Paralelo/Paralelo.csproj 2
dotnet run --project Paralelo/Paralelo.csproj 4
dotnet run --project Paralelo/Paralelo.csproj 8
```

## Visualización

El script de Python `Visualizacion/analisis.py` generará las gráficas comparativas y la animación.

```bash
python Visualizacion/analisis.py
```

## Modelo Matemático

- **Grilla**: 1000x1000 (1 millón de individuos).
- **Estados**: 
  - 0: Susceptible
  - 1: Infectado
  - 2: Recuperado
- **Reglas**:
  - Un Susceptible se infecta con probabilidad $P_{contagio}$ si tiene vecinos infectados. La probabilidad total es $1 - (1 - P_{contagio})^k$, donde $k$ es el número de vecinos infectados.
  - Un Infectado se recupera con probabilidad $P_{recuperacion}$.

## Análisis de Resultados (Ejemplo)

El Speed-up se calcula como $S_p = T_1 / T_p$.
Se espera que el Speed-up aumente con el número de cores, aunque no linealmente debido a la sobrecarga de gestión de hilos y sincronización.
