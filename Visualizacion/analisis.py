import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.animation as animation
import os

def plot_sir_curves():
    # Obtener ruta absoluta del directorio del script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    # La raíz del proyecto es el padre de Visualizacion
    project_root = os.path.dirname(script_dir)
    
    # Carpeta de datos
    data_dir = os.path.join(project_root, 'datos')
    if not os.path.exists(data_dir):
        os.makedirs(data_dir)

    # Buscar archivo secuencial en la carpeta datos
    seq_file = os.path.join(data_dir, 'Resultados_Secuencial.csv')
    
    if not os.path.exists(seq_file):
        print(f"No se encontró {seq_file}")
        print("Asegúrate de ejecutar la simulación secuencial primero.")
        return

    df_seq = pd.read_csv(seq_file)
    
    fig, ax = plt.subplots(figsize=(10, 6))
    
    def update(frame):
        ax.clear()
        # Plot hasta el frame actual (muestreo cada 5 frames para velocidad)
        current_day = frame
        if current_day >= len(df_seq): current_day = len(df_seq) - 1
        
        data = df_seq.iloc[:current_day+1]
        ax.plot(data['Dia'], data['Susceptibles'], label='Susceptibles', color='blue')
        ax.plot(data['Dia'], data['Infectados'], label='Infectados', color='red')
        ax.plot(data['Dia'], data['Recuperados'], label='Recuperados', color='green')
        
        ax.set_title(f'Evolución de la Epidemia (Día {current_day})')
        ax.set_xlabel('Día')
        ax.set_ylabel('Población')
        ax.set_xlim(0, 365)
        ax.set_ylim(0, 1000000)
        ax.legend(loc='upper right')
        ax.grid(True)

    # Crear animación
    ani = animation.FuncAnimation(fig, update, frames=range(0, len(df_seq), 2), interval=50, repeat=False)
    
    # Guardar como mp4 o gif si es posible, sino mostrar
    output_gif = os.path.join(data_dir, 'Evolucion_SIR.gif')
    output_png = os.path.join(data_dir, 'Curvas_SIR_Final.png')
    
    try:
        ani.save(output_gif, writer='pillow')
        print(f"Animación guardada: {output_gif}")
    except Exception as e:
        print(f"No se pudo guardar la animación (falta ffmpeg o pillow?): {e}")
        plt.savefig(output_png)

def plot_speedup():
    # Obtener ruta absoluta del directorio del script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    data_dir = os.path.join(project_root, 'datos')
    
    time_file = os.path.join(data_dir, 'tiempos.csv')
    
    if not os.path.exists(time_file):
        print(f"No se encontró {time_file}")
        print("Asegúrate de ejecutar las simulaciones para generar tiempos.")
        return

    # Leer sin header, asignar nombres
    try:
        df = pd.read_csv(time_file, names=['Type', 'Cores', 'TimeMs'])
    except:
        print("Error leyendo tiempos.csv")
        return

    # Filtrar paralelo
    df_par = df[df['Type'] == 'Paralelo'].sort_values('Cores')
    
    # Obtener tiempo secuencial (base)
    seq_rows = df[df['Type'] == 'Secuencial']
    if seq_rows.empty:
        print("No hay datos secuenciales para calcular speedup.")
        return
        
    t_seq = seq_rows.iloc[0]['TimeMs']
    
    # Calcular Speedup
    df_par['Speedup'] = t_seq / df_par['TimeMs']
    
    plt.figure(figsize=(8, 6))
    plt.plot(df_par['Cores'], df_par['Speedup'], marker='o', linestyle='-', color='b', label='Medido')
    plt.title('Strong Scaling: Speedup vs Cores')
    plt.xlabel('Número de Cores')
    plt.ylabel('Speedup (T_seq / T_par)')
    plt.legend()
    plt.grid(True)
    
    output_speedup = os.path.join(data_dir, 'Speedup.png')
    plt.savefig(output_speedup)
    print(f"Gráfica guardada: {output_speedup}")

if __name__ == "__main__":
    plt.savefig('Speedup.png')
    print("Gráfica guardada: Speedup.png")

if __name__ == "__main__":
    plot_sir_curves()
    plot_speedup()
