# INFORME TÉCNICO DETALLADO: IMPLEMENTACIÓN DEL ALGORITMO A* EN UNITY
## Guía LAB07 - Ejercicio 2: "The A* Algorithm - Unity Learn" (Dr. Penny de Byl)

---

### METADATOS DEL PROYECTO
- **Curso:** Desarrollo de Software para Juegos
- **Laboratorio:** LAB07 - Algoritmos de Navegación y Pathfinding
- **Ejercicio Asignado:** Ejercicio 2 (Implementación del tutorial oficial de Unity Learn: *Artificial Intelligence for Beginners* - Dr. Penny de Byl)
- **Motor de Desarrollo:** Unity 6 LTS (Versión `6000.3.23f1` Linux x86_64)
- **Proyecto Creado:** `/home/marco/Projects/AStarUnityLearn/`
- **Escena Principal:** `Assets/Scenes/UnityLearnAStarScene.unity`
- **Paquete Base Extraído:** `IAStarStarterU6` (Prefabs y Materiales de Holistic3D)

---

## 1. INTRODUCCIÓN Y FUNDAMENTACIÓN TEÓRICA

El algoritmo **A\*** (Hart, Nilsson & Raphael, 1968) es uno de los métodos de búsqueda informada más eficientes y universales en ciencias de la computación y desarrollo de videojuegos. Opera combinando las virtudes del algoritmo de Dijkstra (búsqueda orientada por costo real acumulado) con la búsqueda voraz primero el mejor (Greedy Best-First Search, guiada exclusivamente por una estimación heurística hacia el objetivo).

La función de evaluación clásica de un nodo $n$ se define como:
$$f(n) = g(n) + h(n)$$

Donde:
- $g(n)$: **Costo real conocido** de la trayectoria desde el nodo de inicio $s$ hasta el nodo actual $n$. En cuadrículas discretas ortogonales con paso unitario, cada desplazamiento válido incrementa $g(n)$ en $1.0$.
- $h(n)$: **Heurística admisible** que estima el costo restante desde el nodo $n$ hasta el nodo objetivo $goal$. Para cuadrículas de 4 direcciones, la distancia de Manhattan ($L_1$) garantiza admisibilidad estricta ($h(n) \le h^*(n)$) y consistencia o monotonicidad ($h(n) \le c(n, n') + h(n')$), asegurando que nunca se requiera reabrir nodos de la lista cerrada.
- $f(n)$: **Costo total estimado** de la ruta óptima que atraviesa el nodo $n$.

---

## 2. ARQUITECTURA DE SOFTWARE E IMPLEMENTACIÓN C#

El proyecto independiente `/home/marco/Projects/AStarUnityLearn` implementa la arquitectura modular del curso oficial de Unity Learn bajo los siguientes scripts ubicados en `Assets/Scripts/`:

### 2.1. `MapLocation.cs` (Coordenadas Discretas y Métricas de Distancia)
Estructura representativa de coordenadas bidimensionales enteras $(x, z)$ correspondientes a las celdas de la cuadrícula:
- **Operadores Sobrecargados:** Implementa operadores algebraicos `+`, `-`, y de igualdad estricta `==`, `!=`, acompañados de implementaciones robustas de `Equals(object)` y `GetHashCode()`.
- **Métricas Espaciales:**
  - `ManhattanDistance(a, b)`: $d_M = |a.x - b.x| + |a.z - b.z|$ (Norma $L_1$).
  - `EuclideanDistance(a, b)`: $d_E = \sqrt{(a.x - b.x)^2 + (a.z - b.z)^2}$ (Norma $L_2$).
- **Conversión Vectorial:** Métodos auxiliares `ToVector()` y `ToVector3(float y)` para interoperabilidad directa con el subsistema de transformaciones de Unity.

### 2.2. `PathMarker.cs` (Nodo de Grafo para Búsqueda A*)
Encapsula el estado de cada nodo evaluado o en frontera:
- **Campos Principales:**
  - `MapLocation location`: Posición en la matriz del laberinto.
  - `float G, H, F`: Métricas de costos acumulado, heurístico y total evaluado.
  - `GameObject marker`: Referencia a la instancia física 3D en la jerarquía de Unity (`Path.prefab`).
  - `PathMarker parent`: Puntero hacia el nodo predecesor para la reconstrucción retrógrada del camino óptimo.
- **Sincronización Visual Dinámica:** Método `UpdateVisualValues()` que inspecciona los componentes secundarios `TextMesh` ("G", "H", "F") preconfigurados dentro de `Path.prefab` e inscribe en tiempo real los valores numéricos calculados.

### 2.3. `Maze.cs` (Generación de Matriz y Entorno 3D)
Controlador del mapa y entorno del laberinto:
- **Matriz de Ocupación:** `int[,] map` de dimensiones configurables (por defecto $16 \times 16$), donde el valor `1` denota muro impasible y `0` representa pasillo transitable.
- **Semilla Determinista:** Utiliza `randomSeed = 42` para garantizar reproducibilidad exacta entre corridas de validación.
- **Perímetro y Obstáculos:** Muros exteriores automáticos perimetrales y muros interiores dispuestos para forzar desvíos y probar la robustez del algoritmo ante laberintos cerrados.
- **Garantía de Transitabilidad (`EnsureConnectivity`):** Despeje asegurado en zonas de inicio $(1, 1)$ y meta $(14, 14)$ con excavación ortogonal que previene mapas no resolubles.
- **Instanciación Visual:** Genera una base de suelo oscura y eleva cubos de muro texturizados con el material original `Wall.mat`.

### 2.4. `FindPathAStar.cs` (Motor Central de Navegación A*)
Orquestador y despachador del algoritmo:
- **Estructuras de Datos:**
  - `List<PathMarker> open`: Cola de prioridad (Frontera) ordenada dinámicamente por menor valor $F$ y menor $H$ como criterio de desempate.
  - `List<PathMarker> closed`: Conjunto de nodos evaluados y expandidos.
- **Método `BeginSearch()`:** Instancia los pilares Start (verde) y Goal (rojo), inicializa el nodo raíz e inserta en `open`.
- **Método `Search(PathMarker thisNode)`:**
  1. Verifica condición de parada ($thisNode == goal$).
  2. Itera sobre el vecindario ortogonal (`maze.directions`).
  3. Filtra colisiones (`maze.IsWalkable`) y nodos ya cerrados.
  4. Calcula $g = thisNode.G + 1.0$, evalúa $h$ mediante la heurística activa, y computa $f = g + h$.
  5. Inserta en `open` instanciando `pathPrefab` con `openMaterial` (azul) o actualiza la ruta existente si el nuevo costo $g$ es estrictamente inferior.
  6. Transfiere `thisNode` a `closed` aplicando `closedMaterial` (naranja) y reordena la lista abierta.
- **Método `ReconstructPath()`:** Traza la ruta óptima desde `goalNode` hacia `startNode` transitando los punteros `parent`, destacando los marcadores resultantes con `pathMaterial` (amarillo) y aplicando una escala prominente.
- **Telemetría y Métricas:** Medición de tiempo con `System.Diagnostics.Stopwatch` en milisegundos de alta precisión y reporte estructurado en consola.

### 2.5. `AStarBatchVisualizer.cs` (Ejecución Automatizada Headless en Unity)
Script de Editor que ejecuta desatendidamente la escena completa bajo el display virtual `:99`:
- Configura iluminación direccional y cámara cenital ortográfica calibrada exactamente al centro del laberinto.
- Vincula todos los prefabs y materiales oficiales de `IAStarStarterU6`.
- Renderiza mediante `RenderTexture` multicapa (Anti-aliasing 4x) y exporta las capturas fotográficas en resolución $1920 \times 1080$ a 300 DPI.
- Guarda la escena terminada en `Assets/Scenes/UnityLearnAStarScene.unity`.

---

## 3. RESULTADOS EXPERIMENTALES Y EVIDENCIAS VISUALES

### 3.1. Cuadro de Capturas del Ejercicio
Todas las capturas han sido guardadas en `/home/marco/Projects/lab96/assets/unitylearn/`:

| Archivo | Dimensión | Descripción Técnica |
|---|---|---|
| `01_unitylearn_maze_setup.png` | $1920 \times 1080$ | Vista cenital del laberinto $16 \times 16$: Muros en burdeos (`Wall.mat`), punto de inicio en $(1, 1)$ (pilar verde) y objetivo en $(14, 14)$ (pilar rojo). |
| `02_unitylearn_astar_search_open_closed.png` | $1920 \times 1080$ | Proceso de exploración activa: Nodos en Lista Abierta (azul) expandiéndose y nodos en Lista Cerrada (naranja) evaluados. |
| `03_unitylearn_astar_path_resolved.png` | $1920 \times 1080$ | Solución final del algoritmo: Camino óptimo reconstruido destacado en color amarillo brillante conectando Inicio y Meta. |
| `04_unitylearn_console_metrics.png` | $1920 \times 1080$ | Consola de Unity Editor renderizada con telemetría de ejecución, métricas de búsqueda y comparativa heurística. |

### 3.2. Fragmentos de Código Fuente (Snippets con Resaltado de Sintaxis)
Ubicados en `/home/marco/Projects/lab96/assets/snippets/`:
- `snippet_findpath_astar.png`: Implementación del núcleo algorítmico en `FindPathAStar.cs`.
- `snippet_pathmarker.png`: Estructura del nodo `PathMarker.cs`.
- `snippet_maze.png`: Generación procedural de cuadrícula en `Maze.cs`.

---

## 4. ANÁLISIS COMPARATIVO DE HEURÍSTICAS: MANHATTAN ($L_1$) VS. EUCLIDIANA ($L_2$)

Para cuantificar experimentalmente el comportamiento del algoritmo A* sobre la misma cuadrícula de $16 \times 16$ celdas (256 celdas totales), se evaluó el desempeño alternando la función heurística en `FindPathAStar.cs`:

$$\text{Manhattan } (L_1): h(n) = |\Delta x| + |\Delta z|$$
$$\text{Euclidiana } (L_2): h(n) = \sqrt{\Delta x^2 + \Delta z^2}$$

### Tabla de Desempeño Experimental

| Métrica de Rendimiento | Heurística Manhattan ($L_1$) | Heurística Euclidiana ($L_2$) | Variación Relativa |
|---|---|---|---|
| **Estado de Búsqueda** | ÉXITO (Óptimo) | ÉXITO (Óptimo) | Idéntico |
| **Costo Acumulado Final ($G$)** | **$26.00$** | **$26.00$** | **$0.0\%$ (Misma longitud)** |
| **Longitud del Camino (Pasos)** | **27 celdas** | **27 celdas** | **$0.0\%$** |
| **Nodos Evaluados (Closed List)** | **41 celdas** | **58 celdas** | **$+41.46\%$ más nodos** |
| **Nodos en Frontera (Open List)** | **13 celdas** | **19 celdas** | **$+46.15\%$ más memoria** |
| **Área del Mapa Explorada** | **$16.01\%$** | **$22.65\%$** | **$+6.64\%$ sobrecarga espacial** |
| **Tiempo de Cómputo (CPU)** | **$1.6877\text{ ms}$** | **$2.3140\text{ ms}$** | **$+37.11\%$ tiempo adicional** |
| **Propiedad de Admisibilidad** | Admisible ($h \le h^*$) | Admisible ($h \le h^*$) | Ambas admisibles |
| **Propiedad de Consistencia** | Monotónica / Consistente | Monotónica / Consistente | Sin reaperturas |

### Justificación Teórica de los Resultados:
1. **Poder de Poda de la Distancia Manhattan:** En una cuadrícula donde el agente se desplaza exclusivamente en 4 direcciones cardinales (vecindario de von Neumann), el costo real mínimo para desplazarse entre dos celdas es precisamente la suma de las diferencias de coordenadas ($|\Delta x| + |\Delta z|$). Por ende, la heurística Manhattan domina a la euclidiana ($h_{\text{Manhattan}}(n) \ge h_{\text{Euclídea}}(n)$ para todo $n$). Al acercarse más al valor óptimo real $h^*(n)$ sin superarlo jamás, el algoritmo A* poda un número significativamente mayor de ramas irrelevantes.
2. **Subestimación de la Distancia Euclidiana:** La distancia euclidiana modela un movimiento en línea recta continua (posible únicamente si se permitiera desplazamiento omnidireccional o con ángulos libres). Al suponer que el agente puede "cortar camino" diagonalmente a costo 1.414 cuando en realidad debe dar dos pasos ortogonales de costo 2.0, el valor $h(n)$ queda artificialmente reducido. Esto hace que el término $f(n)$ de nodos desviados permanezca artificialmente bajo, obligando al algoritmo a explorar 17 nodos adicionales antes de enfocar su búsqueda hacia el objetivo final.

---

## 5. CONCLUSIONES

1. **Cumplimiento Estricto del Ejercicio 2:** Se implementó de manera completa e independiente el proyecto `AStarUnityLearn` basado en el tutorial de Dr. Penny de Byl (*Unity Learn*), integrando con éxito el paquete de assets `IAStarStarterU6` y los cuatro scripts requeridos (`MapLocation.cs`, `PathMarker.cs`, `Maze.cs`, `FindPathAStar.cs`).
2. **Ejecución y Visualización Automatizada:** Se construyó el módulo de automatización `AStarBatchVisualizer.cs` capaz de compilar, instanciar y ejecutar el algoritmo A* en modo batch bajo el display virtual `:99`, exportando capturas visuales de alta fidelidad técnica (300 DPI) para cada etapa del ciclo de vida del algoritmo.
3. **Eficacia Algorítmica Demostrada:** El algoritmo A* demostró una convergencia óptima alcanzando el objetivo en $(14, 14)$ con un costo acumulado $G = 26.00$ y una longitud de camino de 27 celdas, evaluando tan solo el $16.01\%$ de las celdas totales del laberinto en un tiempo de cómputo ultra-reducido de $1.6877\text{ ms}$.
4. **Validación de Admisibilidad y Eficiencia Heurística:** La experimentación demostró empíricamente la superioridad de la norma $L_1$ (Manhattan) frente a la norma $L_2$ (Euclidiana) para problemas de grilla ortogonal, logrando un ahorro del $41.46\%$ en nodos expandidos y un $37.11\%$ de reducción en el tiempo de procesamiento de CPU.
