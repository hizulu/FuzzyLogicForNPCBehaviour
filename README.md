<div align="center">
  
# Trabajo de Fin de Grado de Lucía García López
### Uso de la lógica difusa para el comportamiento de *NPCs*
</div>

Este repositorio contiene la implementación del sistema de inteligencia artificial basado en **Lógica Difusa (Fuzzy Logic)** para el comportamiento de NPCs (personajes no jugables) en videojuegos.

Este proyecto fue desarrollado como parte del **Trabajo Fin de Grado (TFG)**: *"Uso de la lógica difusa para el comportamiento de NPCs"* en la Universidad de Burgos, enfocado en crear comportamientos animales realistas, dinámicos y sostenibles.

## Arquitectura del Proyecto

El sistema está compuesto por una arquitectura modular donde se separa la configuración de datos (ScriptableObjects), el cálculo matemático y el control de comportamiento.

### Guía de Scripts

| Script | Rol | Descripción |
| :--- | :--- | :--- |
| `FuzzyAnimalController.cs` | **Núcleo** | Controlador principal. Integra sensores, cálculo de lógica difusa, comunicación con `NavMeshAgent` y ejecución de animaciones. |
| `AnimalBase.cs` | **Data (SO)** | `ScriptableObject` que almacena los parámetros físicos y de personalidad (sensibilidad al miedo, tasas de decaimiento, velocidades). |
| `RuleTables.cs` | **Inferencia** | Define la base de reglas (If-Then). Convierte las categorías difusas (tags) en acciones concretas (`FastFlee`, `SlowApproach`, etc.). |
| `Defuzzification.cs` | **Matemáticas** | Implementa el método de centro de gravedad/promedio ponderado para convertir los grados de pertenencia en un valor decimal utilizable. |
| `MembershipFunction.cs` | **Matemáticas** | Contiene las funciones de pertenencia (Triangular, Trapezoidal, Boolean, etc.) necesarias para la fuzzificación. |
| `PointOfInterest.cs` | **Entorno** | Define puntos interactuables (comida) en el mapa con radios de influencia y filtrado de especies permitidas. |
| `AnimalController.cs` | **Auxiliar** | Sistema inicial para la gestión de movimiento aleatorio y estados de espera (Idle). Actualmente no se usa.|
| `PlayerController.cs` | **Jugador** | Maneja el movimiento del jugador y la interacción con el entorno (input del teclado). |

## Funcionamiento del Flujo Difuso

El ciclo de decisión del agente sigue un proceso estándar de lógica difusa implementado en el script `FuzzyAnimalController`:

1.  **Fuzzificación:** Los valores numéricos (Distancia, Miedo, Curiosidad) se procesan mediante `MembershipFunction` para obtener grados de pertenencia a conjuntos.
2.  **Inferencia:** El script `RuleTables` evalúa los grados de pertenencia y selecciona la acción dominante según las reglas definidas.
3.  **Defuzzificación:** El script `Defuzzification` normaliza los resultados de las reglas activas para determinar la intención final de movimiento.
4.  **Acción:** El `NavMeshAgent` ejecuta el movimiento ajustando su velocidad y objetivo basándose en la intención resultante.
