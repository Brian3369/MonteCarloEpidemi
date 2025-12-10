import pandas as pd
import matplotlib.pyplot as plt
import glob
import os

def plot_sir_curves():
    # Buscar archivo secuencial
    seq_file = '../Resultados_Secuencial.csv'
    if not os.path.exists(seq_file):
        print("No se encontró Resultados_Secuencial.csv")
        return

    df_seq = pd.read_csv(seq_file)
    
    plt.figure(figsize=(10, 6))
    plt.plot(df_seq['Dia'], df_seq['Susceptibles'], label='Susceptibles', color='blue')
    plt.plot(df_seq['Dia'], df_seq['Infectados'], label='Infectados', color='red')
    plt.plot(df_seq['Dia'], df_seq['Recuperados'], label='Recuperados', color='green')
    
    plt.title('Curvas SIR - Simulación Secuencial')
    plt.xlabel('Día')
    plt.ylabel('Población')
    plt.legend()
    plt.grid(True)
    plt.savefig('Curvas_SIR_Secuencial.png')
    print("Gráfica guardada: Curvas_SIR_Secuencial.png")

def plot_speedup():
    # Buscar archivos paralelos
    files = glob.glob('../Resultados_Paralelo_*.csv')
    if not files:
        print("No se encontraron archivos de resultados paralelos.")
        return

    # Asumimos que tenemos los tiempos de ejecución. 
    # Como el CSV solo tiene datos diarios, necesitamos el tiempo total.
    # El código C# imprime el tiempo en consola. 
    # Para este script, vamos a simular o pedir al usuario que ingrese los tiempos,
    # o mejor, modificaremos el C# para que guarde un archivo de tiempos 'tiempos.csv'.
    
    # Por ahora, generaremos una gráfica placeholder o basada en nombres de archivo si tuvieramos el tiempo en el nombre.
    pass

if __name__ == "__main__":
    plot_sir_curves()
