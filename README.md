# Lupa Liviana

Lupa de pantalla para Windows para personas con visión reducida. Amplía **todo el
escritorio**, así que funciona igual en el navegador, el Explorador, el menú
Inicio, cualquier programa y juegos en **modo ventana sin bordes**. Es un programa
mínimo que vive **solo en la bandeja del sistema**, sin ventana principal: se
configura con el menú del ícono (click derecho o click izquierdo) y usa la API
oficial de ampliación de Windows.

- Un único botón de zoom (tecla o botón lateral del mouse). Por defecto: **F9**.
- Mantener el botón: el zoom sigue al mouse.
- Tocar el botón: el zoom queda fijo en el centro del monitor donde está el mouse.
- Filtros de color para mejorar el contraste.
- Atajos globales para subir y bajar el nivel de zoom.
- **Todas las teclas se configuran desde el menú**, sin editar archivos.
- Sin hooks, sin inyección de código y sin tocar otros procesos.

---

## Descargar

Las versiones listas para usar están en
**[Releases](https://github.com/joelazle/LUPazle/releases/latest)**:

- **`LupaLiviana-...-win-x64.exe`** — autocontenido (unos 63 MB): no requiere
  instalar nada, solo Windows 10/11 x64.
- **`LupaLiviana-...-win-x64-requiere-dotnet8.exe`** — unos 180 KB; necesita el
  [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0).

> El programa no está firmado digitalmente. La primera vez, Windows puede mostrar
> el aviso "Windows protegió su PC". Para abrirlo: **Más información → Ejecutar
> de todas formas**. El código fuente está en este repositorio, así que podés
> revisarlo o compilarlo vos mismo (ver más abajo).

---

## Requisitos

- Windows 10 u 11 (x64).
- Para compilar: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Para ejecutar la versión **dependiente del framework**: .NET 8 **Desktop Runtime** (x64).
  La versión autocontenida no necesita nada instalado.

---

## Compilar y probar

Desde la carpeta raíz del proyecto:

```powershell
dotnet build -c Release
dotnet test
```

`dotnet build` deja el ejecutable en
`src\LupaLiviana\bin\Release\net8.0-windows\LupaLiviana.exe`.

---

## Publicar un único .exe

### Opción 1: dependiente del framework (muy chico)

```powershell
dotnet publish src\LupaLiviana\LupaLiviana.csproj -c Release -r win-x64 `
  --self-contained false -p:PublishSingleFile=true -p:DebugType=None `
  -o publicacion
```

Resultado: `publicacion\LupaLiviana.exe` (unos **210 KB**). Necesita el
.NET 8 Desktop Runtime instalado en la máquina.

### Opción 2: autocontenido (no requiere runtime)

```powershell
dotnet publish src\LupaLiviana\LupaLiviana.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None `
  -o publicacion-autocontenido
```

Resultado: `publicacion-autocontenido\LupaLiviana.exe` (unos **63 MB**, comprimido).
Funciona en cualquier Windows 10/11 x64 sin instalar nada.

> La compresión de un solo archivo solo está disponible en la publicación
> autocontenida; por eso la opción 1 no la usa.

---

## Cómo se usa

Al abrir el programa aparece **solo un ícono de lupa en la bandeja del sistema**
(junto al reloj). Si no lo ves, buscá en la flechita de los íconos ocultos.

Click derecho (o click izquierdo) sobre el ícono abre el menú:

- **Activar Lupa Liviana**: habilita o deshabilita todo el programa.
- **Nivel de zoom**: 1.5x, 2x, 3x, 4x, 6x, 8x.
- **Botón de zoom**: muestra el botón actual y permite cambiarlo.
- **Sensibilidad del toque**: 150, 250 o 400 ms.
- **Suavizado**: sin suavizado, bajo o alto.
- **Filtro de color**: ninguno, invertir, escala de grises o alto contraste.
- **Configurar teclas…**: ventana para cambiar el botón de zoom y todos los atajos.
- **Iniciar con Windows**: agrega o quita la entrada en `HKCU\...\Run`.
- **Abrir archivo de configuración**: abre el JSON en el editor predeterminado.
- **Salir**.

El ícono cambia de color (azul → verde) cuando el zoom está activo, y el texto
del globo del ícono indica el nivel y el estado (`apagado`, `siguiendo al mouse`,
`fijo al centro`).

### El botón de zoom (lo más importante)

Hay un solo botón, y funciona como una palanca:

| Acción | Resultado |
|---|---|
| Zoom apagado + presionar | El zoom se enciende **al instante** y sigue al mouse. |
| Mantener presionado | El zoom sigue al cursor (con suavizado opcional). |
| Soltar antes del umbral (toque corto) | El zoom **queda fijo en el centro** del monitor donde está el mouse. |
| Soltar después del umbral (mantenido) | El zoom se apaga. |
| Estando fijo + presionar | El zoom se apaga en ese momento. |

En resumen: **mantener = seguir al mouse; tocar = dejar el zoom fijo al centro
hasta volver a tocar**. Un toque corto deja la lupa "puesta", como una palanca;
otro toque la saca.

La tecla **no se bloquea ni se consume**: sigue llegando al programa que tenga el
foco normalmente. Por eso conviene elegir un botón que ese programa no use (por
ejemplo, F9 suele estar libre; Escape o la barra espaciadora casi nunca lo están).

### Usar la lupa en Windows

No hace falta estar en un juego: el zoom se aplica a **todo el escritorio** y el
botón funciona en cualquier ventana, aunque el navegador o el Explorador tengan
el foco.

1. Mantené el botón de zoom y mové el mouse para recorrer la pantalla.
2. Soltá el botón para volver a 1× y hacer clic donde quieras.
3. Para dejar la vista fija mientras leés, tocá el botón; otro toque la apaga.

> **Mientras el zoom está puesto, los clics siguen en coordenadas reales.** Es una
> limitación de Windows: para que un clic llegue al elemento ampliado hay que usar
> `MagSetInputTransform`, que exige privilegios UIAccess (una aplicación firmada).
> Por eso conviene **mirar con el zoom y soltar antes de hacer clic**. El modo fijo
> sirve para leer; tocá el botón para apagarlo antes de interactuar.

En varios monitores, el centrado toma como referencia el monitor donde está el
cursor y la vista puede pasar de una pantalla a otra sin recortarse (ver
[Varios monitores](#varios-monitores)).

### Cambiar el botón

1. Click derecho en el ícono → **Botón de zoom** → **Cambiar botón…**
2. Presioná la tecla que quieras, o un **botón lateral del mouse** (atrás/adelante).
3. `Escape` cancela. Si no presionás nada en 10 segundos, se cancela solo.

Se acepta cualquier tecla del teclado y los botones laterales `XButton1` y
`XButton2`. Los botones izquierdo, derecho y central del mouse no se usan porque
interrumpirían el uso normal del equipo. Si mantenés `Ctrl`, `Alt` o `Shift`
mientras elegís la tecla, el modificador se ignora y se guarda solo la tecla
(por ejemplo, `Ctrl+F9` deja `F9` como botón).

### Configurar todas las teclas

Menú del ícono → **Configurar teclas…** abre una ventana con el botón de zoom y
los cuatro atajos globales:

| Acción | Valor por defecto |
|---|---|
| Botón de zoom | `F9` |
| Activar o desactivar Lupa Liviana | *(sin atajo)* |
| Subir el nivel de zoom | `Ctrl+Alt+↑` |
| Bajar el nivel de zoom | `Ctrl+Alt+↓` |
| Siguiente filtro de color | *(sin atajo)* |

Para cambiar uno: **Cambiar**, y después presioná la tecla o combinación.
`Escape` cancela la captura; si no elegís nada en 10 segundos, se cancela sola.
**Quitar** deja el atajo sin asignar. **Restablecer valores** carga los valores
de fábrica. Nada se aplica hasta que apretás **Guardar**.

Al guardar, el programa prueba cada combinación con el registro de atajos de
Windows. Si otro programa ya la está usando, muestra el error y conserva la
combinación anterior. Los atajos también se aplican a todo el sistema, incluso
sobre el juego o el navegador, así que conviene elegir combinaciones que esos
programas no usen.

> El atajo de **activar o desactivar** queda registrado aunque la lupa esté
> apagada: es la forma de volver a encenderla sin el mouse.

### Atajos globales de zoom

Mientras el programa está activo:

- **Ctrl + Alt + Flecha arriba**: sube el nivel de zoom.
- **Ctrl + Alt + Flecha abajo**: baja el nivel de zoom.

Funcionan aunque el juego o el navegador tengan el foco. Son opcionales: si otro
programa ya los usa (por ejemplo, el atajo de rotar pantalla del controlador
Intel), el programa avisa que no pudo registrarlos y se pueden cambiar desde
**Configurar teclas…** o directamente en el archivo de configuración.

También se pueden asignar estos dos atajos opcionales, que por defecto están
vacíos (deshabilitados):

- **Activar o desactivar Lupa Liviana**: hace lo mismo que el primer ítem del
  menú. Sigue funcionando con la lupa apagada.
- **Siguiente filtro de color**: pasa de `Ninguno` a `Invertir`, a
  `Escala de grises`, a `Alto contraste` y vuelve a empezar. Se aplica al
  instante si el zoom está encendido, y muestra el nombre del filtro en un globo.

---

## Varios monitores

La lupa usa el **escritorio virtual completo**. El seguimiento y el centrado toman
como referencia el monitor donde está el cursor, y la vista puede pasar de un
monitor a otro sin recortarse en el borde de la pantalla principal.

- Mantener el botón: el cursor se muestra en el centro del monitor donde está.
- Tocar el botón: la vista fija se centra en ese mismo monitor.
- Cambiar el nivel de zoom estando fijo no mueve la vista a otro monitor aunque
  el mouse se haya movido.

La API de Windows aplica un mismo nivel de zoom a todas las pantallas. Con un solo
monitor el comportamiento es exactamente el mismo de siempre.

---

## Juegos: modo ventana sin bordes

La lupa usa la API de ampliación de **pantalla completa** de Windows, que amplía el
escritorio. Por eso:

- El juego debe estar en **modo ventana sin bordes** (borderless windowed).
- **La pantalla completa exclusiva (exclusive fullscreen) no está soportada**:
  en ese modo el juego dibuja directo en la pantalla y la lupa no lo puede ampliar.
- Si el juego usa su propio modo "pantalla completa" con cambio de resolución, la
  lupa se ve mal o no se ve.

El programa no instala hooks ni inyecta código en otros procesos, así que no
interfiere con sistemas antitrampas; aun así, ningún programa externo puede
garantizar al 100 % que un antitrampas lo acepte.

---

## Configuración (JSON)

El archivo se crea solo en:

```
%AppData%\LupaLiviana\config.json
```

Ejemplo con todos los campos:

```json
{
  "nivelZoom": 2,
  "botonZoom": "F9",
  "umbralToqueMs": 250,
  "suavizado": "Bajo",
  "filtro": "Ninguno",
  "atajoSubirZoom": "Ctrl+Alt+Up",
  "atajoBajarZoom": "Ctrl+Alt+Down",
  "atajoActivar": "",
  "atajoSiguienteFiltro": "",
  "iniciarConWindows": false,
  "activado": true
}
```

Valores válidos:

- `nivelZoom`: entre `1.5` y `8`.
- `botonZoom`: una tecla (`"F9"`, `"F8"`, `"Space"`, `"Oemtilde"`, …) o
  `"MouseXButton1"` / `"MouseXButton2"`.
- `umbralToqueMs`: entre 80 y 2000.
- `suavizado`: `"Ninguno"`, `"Bajo"` o `"Alto"`.
- `filtro`: `"Ninguno"`, `"Invertir"`, `"EscalaDeGrises"` o `"AltoContraste"`.
- `atajoSubirZoom` / `atajoBajarZoom` / `atajoActivar` / `atajoSiguienteFiltro`:
  modificadores (`Ctrl`, `Alt`, `Shift`, `Win`) más una tecla, por ejemplo
  `"Ctrl+Alt+Up"` o `"Ctrl+Shift+F1"`. Un texto vacío (`""`) deshabilita ese
  atajo; en los dos atajos de nivel, un texto con formato inválido vuelve al
  valor por defecto, y en los opcionales queda deshabilitado.
- `iniciarConWindows`: `true` o `false`.
- `activado`: `true` o `false`.

La configuración se guarda sola en cada cambio hecho desde el menú. Si editás el
archivo a mano, los cambios se aplican al **reiniciar** el programa. Si el archivo
está dañado, se usan los valores por defecto y se guarda una copia del original
como `config.json.roto`.

---

## Cómo funciona (notas técnicas)

- **API de lupa**: `magnification.dll` (`MagInitialize`, `MagUninitialize`,
  `MagSetFullscreenTransform`, `MagGetFullscreenTransform`, `MagSetColorEffect`).
  No se usa `MagSetInputTransform`: requiere privilegios UIAccess (una aplicación
  firmada), así que los clics se hacen en coordenadas reales. La lupa se usa para
  mirar y se suelta antes de hacer clic.
- **Botón de zoom**: se lee con `GetAsyncKeyState` desde un temporizador de la
  interfaz. Windows cuantiza los temporizadores a ~15,6 ms, por lo que se usa un
  intervalo de 10 ms y se obtienen ~60 lecturas por segundo. La repetición
  automática del teclado se ignora comparando el estado anterior.
- **Señales de presionar/soltar sin hooks**: no se usa `SetWindowsHookEx` ni se
  inyecta nada en otros procesos.
- **Seguimiento del mouse**: `GetCursorPos` en el mismo temporizador, con
  interpolación opcional y sin reservar memoria en cada tick. El desplazamiento se
  limita a los bordes del escritorio virtual.
- **Geometría de varios monitores**: `EscritorioWindows` consulta
  `SM_*VIRTUALSCREEN`, `MonitorFromPoint` y `GetMonitorInfo`; `CalculoVista` hace
  el cálculo puro del desplazamiento y se prueba con monitores simulados.
- **DPI alto**: `app.manifest` declara `PerMonitorV2`; las coordenadas que se usan
  son píxeles físicos.
- **Instancia única**: `Mutex` con nombre; si ya hay una instancia, avisa y se
  cierra.
- **Seguridad de pantalla**: al apagar el zoom, al salir, ante una excepción no
  controlada y al cerrar la sesión de Windows se vuelve el zoom a `1.0` y se llama
  a `MagUninitialize`. Al arrancar también se corrige cualquier zoom que haya
  quedado de una ejecución anterior.
- **Íconos**: se dibujan por código en tiempo de ejecución (no hay archivos de
  imagen); hay una variante normal y otra para cuando el zoom está activo.
- **Antitrampas**: el programa solo consulta el estado de las teclas y usa APIs
  oficiales del sistema; no abre, lee ni modifica la memoria de otros procesos.

---

## Estructura del proyecto

```
LupaLiviana.sln
src/LupaLiviana/
  Program.cs              Punto de entrada, instancia única y manejo de errores.
  TrayApp.cs              ApplicationContext: ícono, menú, timer y comandos.
  MagnifierService.cs     Envoltorio de la API de lupa y matrices de color.
  VistaEscritorio.cs      Geometría del escritorio virtual y de cada monitor.
  ButtonStateMachine.cs   Lógica de mantener/tocar, sin acceso a Windows.
  ConfigService.cs        Modelo de configuración y lectura/escritura del JSON.
  NativeMethods.cs        Declaraciones P/Invoke.
  BotonZoom.cs            Botón de zoom (tecla o botón lateral) y su texto.
  Teclas.cs               Nombres de teclas y detección de modificadores.
  Atajo.cs                Atajos globales: lectura, escritura y texto visible.
  RegistroAtajos.cs       Altas y bajas de atajos, con vuelta atrás si falla.
  CapturaTeclas.cs        Captura de la próxima tecla o combinación, sin hooks.
  ConfigTeclas.cs         Conjunto de teclas configurable y sus validaciones.
  FormularioTeclas.cs     Ventana "Configurar teclas".
  VentanaAtajos.cs        Ventana invisible que recibe WM_HOTKEY.
  IconoTray.cs            Generación de los íconos de la bandeja.
  app.manifest            DPI PerMonitorV2 y ejecución sin privilegios.
tests/LupaLiviana.Tests/
  ButtonStateMachineTests.cs   Toque corto, mantenido, toque estando fijo,
                               repetición de tecla y umbral.
  BotonYAtajosTests.cs         Botones, atajos y su texto canónico.
  ConfigServiceTests.cs        Valores por defecto, ida y vuelta, archivo roto.
  CapturaTeclasTests.cs        Combinaciones, Escape, timeout y modificadores.
  RegistroAtajosTests.cs       Registro, liberación y vuelta atrás.
  ConfigTeclasTests.cs         Valores por defecto, duplicados y advertencias.
  FiltrosTests.cs              Ciclo y nombres de los filtros de color.
  VistaEscritorioTests.cs      Centrado y límites con uno y varios monitores.
README.md
```

---

## Problemas frecuentes

- **El ícono no aparece**: puede estar en el área de íconos ocultos de la bandeja.
- **El zoom no se ve sobre el juego**: el juego está en pantalla completa
  exclusiva; cambialo a ventana sin bordes.
- **Mientras hay zoom, los clics caen en otro lugar**: es una limitación de
  Windows. La lupa no puede reasignar los clics a la vista ampliada sin
  privilegios UIAccess (aplicación firmada). Soltá el botón o tocá para apagar el
  zoom antes de hacer clic.
- **La vista no sigue al foco del teclado**: la lupa sigue al mouse, no al cursor
  de texto. Si navegás con `Tab`, mové un poco el mouse para llevar la vista a la
  zona nueva.
- **En un monitor secundario la vista se recortaba**: el centrado ahora toma el
  monitor donde está el cursor y los límites son los del escritorio completo.
- **Los atajos Ctrl+Alt+Flechas no hacen nada**: otro programa los tiene tomados
  (controladores de video, herramientas de accesibilidad) o el atajo quedó vacío.
  Se pueden cambiar desde **Configurar teclas…** o en el JSON.
- **Al guardar un atajo dice que no se pudo registrar**: otro programa ya usa esa
  combinación. Probá con otra; el atajo anterior se conserva.
- **El atajo incluye la tecla del botón de zoom**: la ventana avisa. Al usarlo se
  va a mover la lupa además de la acción del atajo; conviene elegir otra
  combinación.
- **Cambié el JSON y no pasa nada**: los cambios se leen al iniciar; cerrá y volvé
  a abrir el programa.
- **La pantalla quedó ampliada**: no debería pasar; el programa restaura el zoom en
  todos los caminos de salida. Si ocurre, ejecutá `LupaLiviana.exe` de nuevo y
  salí desde el menú, o usá la Lupa de Windows (tecla Windows + `+`) y volvé a
  `1.0`.

---

## Licencia

[MIT](LICENSE): podés usar, modificar y redistribuir el programa libremente,
manteniendo el aviso de copyright.
