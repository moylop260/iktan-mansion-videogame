# La Mansión (juego de terror en primera persona)

Idea original de Iktan. Proyecto de Unity 3D para macOS.

## Cómo se juega
- **WASD** mover · **Ratón** mirar · **Shift** correr
- **E** interactuar · **F** linterna (necesita linterna + pilas) · **H** pista · **Esc** pausa
- Las **lámparas azules** son checkpoints: al pasar cerca se guarda el avance (se ponen verdes).
- Si abres el refri equivocado... ¡game over! Reintentas desde el último checkpoint (R) o empiezas de cero (N).

## Camino correcto (spoiler)
Cocina (cajón: linterna) → Cuarto 1 (mesita: pilas) → Baño (lavabo: llave verde) → Biblioteca (libro azul: llave azul;
¡no el rojo!) → reja de la escalera → Cuarto 3 (cómoda: partituras) → Piano (con linterna encendida: llave amarilla)
→ Oficina → caja fuerte (el año en que compraron la casa, lo dice el diario de la Sala) → llave dorada → puerta principal.

## Abrir y compilar
1. Abre Unity Hub → *Add project from disk* → esta carpeta, con Unity 6.
2. Menú **Mansion → Compilar para macOS** genera `Builds/Mansion.app`.
   (o pulsa ▶ Play dentro del editor).

Por línea de comandos:
`"/Applications/Unity/Hub/Editor/<versión>/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath . -executeMethod MansionEditor.BuildMac -logFile build.log`
