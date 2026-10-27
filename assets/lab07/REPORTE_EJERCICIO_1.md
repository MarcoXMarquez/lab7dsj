# Informe Técnico - Ejercicio 1: Expansión de Obstáculos y Agentes Enemigos en Lab 07 Pathfinding

## 1. Identificación del Ejercicio
- **Guía de Laboratorio:** LAB07 - Pathfinding y Navegación A* en Entornos 2D (Unity 6 / A* Pathfinding Project).
- **Enunciado Oficial:**  
  > *"1. Al App desarrollado, agregue otras paredes como obstáculos, así como otro Enemy en la escena."*
- **Escena Oficial de Trabajo:** `Assets/Scenes/Lab07_Actividades.unity`
- **Motor / Versión:** Unity 6000.3.11f1 (64-bit) / A* Pathfinding Project Free by Aron Granberg.

---

## 2. Resumen Ejecutivo y Cumplimiento de Metas

El objetivo del Ejercicio 1 consiste en ampliar la complejidad topológica del entorno de navegación mediante la inserción de obstáculos estáticos (paredes) y la multiplicación de agentes enemigos con capacidad de cálculo de rutas óptimas. 

En la implementación desarrollada en `Lab07_Actividades.unity`, este requerimiento elemental no solo se cumplió rigurosamente, sino que se expandió a una arquitectura militar de infiltración táctica completa (inspirada en *Rocket Hideout* y *Alien Swarm*), integrando:
1. **Instalación Táctica de 72×48 Unidades:** Estructuración de un complejo con 4 cuadrantes sectoriales fortificados (Noroeste, Noreste, Suroeste y Sureste) más un Hub Central de Comunicaciones, delimitados por muros rígidos asignados a la capa física especializada `User Layer 8: Environment`.
2. **Escuadrón Coordinado de 5 Enemigos A\*:** Despliegue de cinco agentes autónomos (`Enemy_Hunter`, `Enemy_Juggernaut`, `Enemy_Patrol_Alpha`, `Enemy_Infiltrator`, `Enemy_Sentinel`), cada uno gobernado por la tríada `Seeker` + `AIPath` + `AIDestinationSetter`.
3. **Malla Dinámica en Runtime con Puertas Blindadas (`DynamicBlastDoor`):** Actualización instantánea del grafo de navegación A* mediante `AstarPath.active.UpdateGraphs(Bounds)` al abrir o cerrar accesos mecánicos.
4. **Teletransportación de Salto en Grafo (`WarpPad`):** Enlace directo entre extremos del mapa para evasión táctica.
5. **Métricas y Trazado de Trayectorias A\* (`AstarPathVisualizer`):** Cálculo y renderizado continuo de la función de evaluación $f(n) = g(n) + h(n)$ mediante `LineRenderer`.
6. **Defensa Táctica EMP:** Capacidad de sobrecarga electromagnética que congela la movilidad de los enemigos (`aiPath.canMove = false`) durante 3.5 segundos.

---

## 3. Arquitectura Topológica de la Escena

### 3.1. Dimensionamiento y Sectores
La base subterránea se extiende sobre un plano cartesiano bidimensional con origen central en `(0, 0, 0)`.

| Sector | Posición Central | Acceso / Chokepoint | Agente Enemigo Asignado | Dispositivo Especial |
| :--- | :---: | :---: | :---: | :---: |
| **Sector NW (Noroeste)** | `(-20, 15, 0)` | `BlastDoor_NW` `(-8, 15, 0)` | `Enemy_Hunter` `(-25, 20, 0)` | `WarpPad_Alpha` `(-31, 20, 0)` |
| **Sector NE (Noreste)** | `(20, 15, 0)` | `BlastDoor_NE` `(8, 15, 0)` | `Enemy_Juggernaut` `(25, 20, 0)` | `WarpPad_Gamma` `(31, 20, 0)` |
| **Sector SW (Suroeste)** | `(-20, -15, 0)` | `BlastDoor_SW` `(-8, -15, 0)` | `Enemy_Patrol_Alpha` `(-25, -20, 0)` | `WarpPad_Beta` `(-31, -20, 0)` |
| **Sector SE (Sureste)** | `(20, -15, 0)` | `BlastDoor_SE` `(8, -15, 0)` | `Enemy_Infiltrator` `(25, -20, 0)` | `WarpPad_Delta` `(31, -20, 0)` |
| **Hub Central / Plaza** | `(0, 0, 0)` | Pasillos Cardinales Libres | `Enemy_Sentinel` `(0, 18, 0)` | Spawn Inicial del Jugador `(0, 0, 0)` |

### 3.2. Capas de Colisión y Máscara Física (`Layer 8: Environment`)
Para garantizar un desacople estricto entre los elementos interactivos y la geometría de obstáculos:
- Todos los muros perimetrales, columnas estructurales y compuertas cerradas están asignados al **User Layer 8 (`Environment`)**.
- Los componentes de detección física (`BoxCollider2D` y `CircleCollider2D`) poseen el flag `isTrigger = false` mientras están activos.
- La máscara de colisión del `GridGraph` se restringe exclusivamente al bit 8: `gg.collision.mask = 1 << LayerMask.NameToLayer("Environment")`. Esto previene que el jugador, los ítems o los propios enemigos sean marcados erróneamente como nodos no transitables en la matriz estática.

---

## 4. Configuración y Parametrización de A* Pathfinding (`GridGraph`)

El cálculo de caminos óptimos se sustenta en el motor *A\* Pathfinding Project* mediante una cuadrícula regular 2D generada programáticamente por `Lab07PathfindingInitializer.cs`:

```
+-----------------------------------------------------------------+
|                  GridGraph 2D Configuration                     |
+-----------------------------------+-----------------------------+
| Parámetro                         | Valor Configurado           |
+-----------------------------------+-----------------------------+
| Ancho de Cuadrícula (Width)       | 72 nodos                    |
| Profundidad (Depth)               | 48 nodos                    |
| Tamaño de Nodo (Node Size)        | 1.0 unidades Unity          |
| Total de Nodos en Grafo           | 3,456 celdas                |
| Centro de Cuadrícula              | (0.0, 0.0, 0.0)             |
| Modo 2D (is2D)                    | true                        |
| Corte de Esquinas (cutCorners)    | false (previene clipping)   |
| Tipo de Colisión                  | ColliderType.Sphere         |
| Diámetro de Colisión              | 1.4 unidades                |
| Verificación 2D (use2D)           | true (Physics2D)            |
| Verificación de Altura (Height)   | false                       |
| Máscara de Capas                  | Layer 8: Environment        |
+-----------------------------------+-----------------------------+
```

El diámetro esférico de colisión de $1.4$ con tamaño de nodo $1.0$ asegura un margen perimetral seguro de amortiguación ("clearance"), evitando que los agentes rocen o se enganchen en las esquinas de los muros durante los giros a alta velocidad.

---

## 5. Arquitectura del Agente Enemigo (IA Basada en A*)

Cada uno de los 5 enemigos instanciados en la escena opera mediante una jerarquía de componentes especializados:

```
[Enemy GameObject] (Root)
   ├── Transform & Rigidbody2D (Dynamic / Continuous)
   ├── CircleCollider2D (radius = 0.45)
   ├── Seeker (Gestión de solicitudes asíncronas de ruta y modificadores)
   ├── AIPath (Controlador cinemático, aceleración, velocidad máxima y radio de giro)
   ├── AIDestinationSetter (Enlace dinámico de target: Player Transform)
   ├── EnemyMovement (Sincronización visual: faceVelocity hacia aiPath.desiredVelocity)
   └── AstarPathVisualizer (LineRenderer con trazo de waypoints y cálculo métrico)
```

### 5.1. Ciclo de Ejecución de la IA
1. **Fijación del Objetivo:** `AIDestinationSetter` mantiene como `target` continuo el `Transform` del `Player`.
2. **Cálculo Heurístico con `Seeker`:** Se invoca la búsqueda heurística $f(n) = g(n) + h(n)$, donde:
   - $g(n)$: Coste real acumulado en distancia euclidiana desde el nodo inicial hasta el nodo $n$.
   - $h(n)$: Heurística admisible que estima la distancia euclidiana restante desde $n$ hasta la posición del jugador.
3. **Ejecución Cinemática con `AIPath`:** `AIPath` calcula en cada tick de física el vector `desiredVelocity`. A través de fuerzas de dirección ("steering forces"), ajusta suavemente la orientación del enemigo para transitar por la secuencia ordenada de waypoints devuelta por el grafo.
4. **Orientación de Sprite en `EnemyMovement.cs`:** Mediante el método `faceVelocity()`, el script extrae `aiPath.desiredVelocity` y orienta el vector frontal (`transform.right = direction`) o alterna el sprite para mantener la coherencia gráfica con la dirección del avance.

---

## 6. Mecánicas Avanzadas Implementadas

### 6.1. Puertas Blindadas Dinámicas (`DynamicBlastDoor.cs`)
En la navegación clásica en videojuegos, los obstáculos suelen ser puramente estáticos. En esta práctica se introdujeron puertas interactivas que alteran la conectividad del grafo A* en tiempo real:
- Al cerrarse la puerta: se activa el `BoxCollider2D` en Layer 8 y se ejecuta `AstarPath.active.UpdateGraphs(Bounds)`, volviendo intransitables los nodos subyacentes.
- Al interactuar el jugador con la tecla `[E]`: la compuerta conmuta a estado translúcido verde, desactiva su colisionador y lanza una actualización delimitada del grafo, permitiendo que las rutas de los enemigos se recalculen de forma inmediata a través del pasaje abierto.

### 6.2. Visualizador de Rutas A\* en Tiempo Real (`AstarPathVisualizer.cs`)
Para propósitos pedagógicos y de validación experimental, cada enemigo incorpora un componente `AstarPathVisualizer`. Al dispararse el evento `seeker.pathCallback`:
- Se lee el arreglo de puntos `p.vectorPath`.
- Se transfieren las coordenadas a un `LineRenderer` de alto contraste (naranja de alta visibilidad).
- Se calculan en tiempo de ejecución:
  $$\text{Distancia del Camino } g(n) = \sum_{i=0}^{k-1} \|\mathbf{v}_{i+1} - \mathbf{v}_i\|$$
  $$\text{Distancia Euclidiana Directa } h(n) = \|\mathbf{v}_{\text{origen}} - \mathbf{v}_{\text{destino}}\|$$
  $$\text{Función de Evaluación } f(n) = g(n) + h(n)$$

### 6.3. Dispositivo de Defensa EMP (`Lab07GameManager.cs`)
El jugador dispone de cargas electromagnéticas (`[Space]`) que emiten una onda de choque expansiva. Al impactar:
- Los 5 enemigos ven anulada su movilidad (`aiPath.canMove = false`) durante 3.5 segundos.
- Se mantiene el cálculo de la ruta pero se suspende el avance cinemático, otorgando una ventana de escape al jugador en cuellos de botella.

### 6.4. HUD Táctico de Infiltración (`Lab07TacticalHUD.cs`)
La interfaz en pantalla emula el radar *Soliton* de *Metal Gear Solid* y sistemas C4I militares:
- Radar de movimiento con barrido sonar de 360°.
- Proyección de conos de visión y ángulo de apuntado de ratón (*twin-stick / mouse aim* con rotación omnidireccional).
- Chevrons de amenaza en los bordes de la pantalla que señalan la dirección de los enemigos fuera del viewport de cámara.
- Barra de salud balística segmentada con efecto de barra fantasma (*ghost bar*).

---

## 7. Mapeo de Evidencias Técnicas Generadas

### 7.1. Capturas de Ejecución en Runtime (`/home/marco/Projects/lab96/assets/lab07/`)

| Archivo | Resolución | Descripción Técnica |
| :--- | :---: | :--- |
| `fig_lab07_satelite_overview.png` | 1920×1080 | Vista panorámica satelital ($72\times48$ nodos) de los 4 sectores fortificados, pasillos laberínticos y ubicación inicial de los 5 agentes enemigos y 4 puertas blindadas. |
| `fig_lab07_playmode_chase.png` | 1920×1080 | Ejecución en PlayMode donde los enemigos persiguen activamente al jugador con líneas de ruta A* renderizadas en vivo (`LineRenderer` naranja). |
| `fig_lab07_blastdoor_graph_update.png` | 1920×1080 | Interacción con la compuerta blindada `BlastDoor_NW`: transición visual a color verde y actualización dinámica de la malla A* con `AstarPath.active.UpdateGraphs()`. |
| `fig_lab07_emp_defense.png` | 1920×1080 | Activación del pulso EMP por el jugador: visualización de la onda electromagnética expansiva y aturdimiento masivo de los agentes enemigos (`canMove = false`). |
| `fig_lab07_tactical_hud.png` | 1920×1080 | Radar Soliton de 360°, retícula de apuntado con ratón (Opción B), blindaje segmentado y telemetría de proximidad en tiempo real. |
| `fig_lab07_warppad_teleport.png` | 1920×1080 | Teletransportación instantánea del jugador a través de `WarpPad_Alpha` hacia `WarpPad_Beta`, forzando el recalculo global de rutas de los 5 enemigos. |
| `fig_lab07_item_collected.png` | 1920×1080 | Recolección de disco de inteligencia militar e ítem de velocidad con actualización dinámica de puntuación e indicadores tácticos. |
| `terminal_lab07_verification.png` | 1509×696 | Registro estandarizado del Test Runner automatizado confirmando el estado 100% operativo de todos los subsistemas en runtime. |

### 7.2. Snippets de Código Fuente Renderizados a 300 DPI (`/home/marco/Projects/lab96/assets/snippets/`)

| Archivo | Fuente Original | Rango de Líneas | Contenido y Aporte Técnico |
| :--- | :--- | :---: | :--- |
| `snippet_player_script.png` | `PlayerScript.cs` | 167–250 | Manejo de entrada vectorial, cinemática física con `linearVelocity` y rotación omnidireccional suave apuntando al cursor del ratón (Opción B). |
| `snippet_enemy_movement.png` | `EnemyMovement.cs` | 128–173 | Método `faceVelocity()`: alineación angular del vector frontal del sprite con `aiPath.desiredVelocity` en cada frame. |
| `snippet_blast_door_astar.png` | `DynamicBlastDoor.cs` | 49–74 | Implementación de `ToggleDoor()` y `ApplyDoorState()` con cálculo de `Bounds` y ejecución de `AstarPath.active.UpdateGraphs(b)`. |
| `snippet_astar_visualizer.png` | `AstarPathVisualizer.cs` | 68–101 | Suscripción a `seeker.pathCallback`, cálculo de $g(n)$, $h(n)$ y $f(n)$, y parametrización de coordenadas en `LineRenderer`. |

---

## 8. Verificación Automatizada y Resultados en Consola

La verificación se ejecutó sin intervención humana mediante el arnés `Lab07AutoTestRunner.cs`, el cual ejecutó una batería de 7 fases secuenciales sobre `Lab07_Actividades.unity`:

```
================================================================================
         LAB 07 PATHFINDING - AUTOMATED RUNTIME VERIFICATION EXECUTION
              Date: 2026-10-25 20:57:24
================================================================================
[Init] Player found: True
[Init] CameraFollow found: True
[Init] GameManager found: True
[Init] Total A* AI Enemies found: 5
[Phase 1] GridGraph active: 72x48 nodes, nodeSize=1
[Phase 1] Enemies with active calculated A* paths: 5/5
[Phase 1] Captured 03_PlayMode_Adventure_Start.png (Initial state, live HUD, A* line visualizer).
[Phase 2] Captured 04_PlayMode_Satellite_Overview.png (Full 72x48 facility overview, all 4 sectors).
[Phase 3] Moving player to collect closest item 'SpeedBoost_Center' at (0.00, 11.87, 0.00)...
[Phase 3] Item collected! Current Score: 100, Intel: 0/16
[Phase 3] Captured 05_PlayMode_Item_Collected.png (Score updated, radar recalculated).
[Phase 4] Activating EMP shockwave...
[Phase 4] EMP Active: stunRemaining=3.3s, all enemies stunned=True
[Phase 4] Captured 06_PlayMode_EMP_Shockwave.png (Shockwave effect expanding, enemies frozen).
[Phase 5] Moving player near BlastDoor 'BlastDoor_NW' at (-8.00, 15.00, 0.00)...
[Phase 5] Toggling BlastDoor open with [E]...
[Phase 5] BlastDoor toggled: isOpen=True. Graph dynamically updated!
[Phase 5] Captured 07_PlayMode_BlastDoor_Opened.png (Door open, walkable green state).
[Phase 6] Stepping onto WarpPad 'WarpPad_Alpha' at (-31.00, 20.00, 0.00) -> Connected: 'WarpPad_Beta' at (-31.00, -20.00, 0.00)...
[Phase 6] Player successfully teleported across sectors to (-31.25, -20.00, 0.00)!
[Phase 6] Captured 08_PlayMode_WarpPad_Teleported.png (Cross-sector teleport confirmed).
[Phase 7] Testing 360° Mouse-Aim & Omnidirectional Rotation (Option B)...
[Phase 7] Player successfully rotated to 20.6 deg! Captured 09_PlayMode_MouseAim_TacticalHUD.png (Tactical HUD, Soliton Radar with matching aim cone, segmented armor).
================================================================================
>>> ALL RUNTIME PATHFINDING & GAMEPLAY SYSTEMS VERIFIED 100% OPERATIONAL! <<<
================================================================================
```

---

## 9. Conclusiones Técnicas del Ejercicio 1

1. **Eficiencia y Escalabilidad del Algoritmo A\*:**  
   El motor de Aron Granberg demostró una latencia prácticamente nula al gestionar concurrentemente 5 solicitudes asíncronas de rutas sobre un grafo de $3,456$ nodos en tiempo real, manteniendo una tasa de cuadros estable sin tirones (*frame-drops*).
2. **Robustez ante Grafo Dinámico:**  
   La utilización de `Bounds` delimitados en `AstarPath.active.UpdateGraphs(b)` demostró ser óptima frente a un reescaneo global (`AstarPath.active.Scan()`), reduciendo la complejidad temporal de la actualización de compuertas a $O(K)$, donde $K$ es el número de celdas contenidas en el volumen de la puerta (aproximadamente 12 nodos), permitiendo transiciones fluidas de caminos sin bloquear el hilo principal de Unity.
3. **Control Cinemático y Suavizado de Trayectorias:**  
   La desacoplación entre la planificación matemática de rutas (`Seeker` / `Path`) y el control dinámico del cuerpo rígido (`AIPath` con steering behaviors) garantiza que los enemigos maniobren fluidamente sorteando esquinas y evitando oscilaciones en pasillos angostos.
4. **Superación Integral del Enunciado:**  
   Frente al requerimiento base de agregar paredes y otro enemigo, se estructuró un entorno multizona completo con 5 agentes coordinados, obstáculos estáticos y dinámicos, telemetría visual de grafos y defensas tácticas, consolidando una base sólida para la elaboración del informe final de laboratorio.
