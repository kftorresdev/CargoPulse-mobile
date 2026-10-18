# Guía de Pruebas End-to-End (E2E) con Maestro - CargoPulse Mobile

> **Documento:** `docs/MAESTRO_E2E_GUIDE.md`  
> **Versión:** 1.0.0  
> **Fecha:** Octubre 2026  
> **Plataforma:** .NET MAUI 9 (Android)  
> **Identificador de Aplicación:** `com.cargopulse.mobile`  
> **Framework E2E:** [Maestro](https://maestro.mobile.dev/)  

---

## 1. Introducción y Estrategia de Pruebas E2E

**Maestro** es el estándar moderno de pruebas funcionales y End-to-End para aplicaciones móviles nativas y multiplataforma (.NET MAUI, Flutter, React Native). A diferencia de Appium o Espresso, Maestro opera con sintaxis **declarativa en YAML**, tolerancia integrada a latencias de red y renderizado, y control fluido del ciclo de vida de la aplicación.

### Beneficios Clave para CargoPulse Mobile
- **Cero Flakiness:** No requiere `sleep` arbitrarios; incluye `extendedWaitUntil` y reintentos adaptativos automáticos.
- **Soporte Nativo para Gestos Táctiles:** Dibuja firmas digitales en `DrawingView` mediante gestos de swipe fluido.
- **Independiente de la Implementación:** Evalúa la interfaz desde la perspectiva del usuario final (accesibilidad, textos y jerarquía visual de UI Automator).
- **Integrable en CI/CD:** Ejecución sin esfuerzo en pipelines de GitHub Actions mediante emuladores headless con aceleración KVM.

---

## 2. Estructura del Suite de Pruebas

El suite de pruebas E2E reside en el directorio `.maestro/` en la raíz del repositorio:

```text
CargoPulse-mobile/
├── .maestro/
│   ├── config.yaml                               # Configuración global y App ID
│   ├── suite.yaml                                # Master runner que orquesta todos los flujos
│   └── flows/
│       ├── 01_auth_flow.yaml                     # Flujo 1: Login, validación y Dashboard
│       ├── 02_manual_search_and_detail_flow.yaml # Flujo 2: Búsqueda manual y Detalle
│       ├── 03_inspection_and_signature_flow.yaml # Flujo 3: Inspección, discrepancia y firma
│       └── 04_offline_sync_flow.yaml             # Flujo 4: Cola SQLite y sincronización
├── docs/
│   └── MAESTRO_E2E_GUIDE.md                      # Esta guía técnica de referencia
└── src/
    └── CargoPulse.Mobile.csproj                  # ApplicationId: com.cargopulse.mobile
```

### Configuración Global (`.maestro/config.yaml`)
```yaml
appId: com.cargopulse.mobile
tags:
  - e2e
  - regression
  - mobile-ops
executionTimeout: 120000
```

---

## 3. Instalación de Maestro CLI

### 3.1 Prerrequisitos
1. **Java Development Kit (JDK):** Versión 11, 17 o 21 instalada y configurada en el `PATH` (`java -version`).
2. **Android SDK Platform-Tools:** Comando `adb` disponible en el `PATH` (`adb version`).
3. **Emulador Android o Dispositivo Físico:** Con depuración USB activa (API 31+ recomendado).

### 3.2 Instalación en Windows
En Windows con PowerShell:
```powershell
# Descarga e instalación directa del CLI oficial de Maestro
Invoke-WebRequest -Uri "https://get.maestro.mobile.dev" -UseBasicParsing | Invoke-Expression

# O mediante WSL2 / Ubuntu (Recomendado para entornos Linux/CI consistentes):
curl -FsSL "https://get.maestro.mobile.dev" | bash
```

Añade Maestro a tus variables de entorno si es necesario:
```powershell
$env:PATH += ";$env:USERPROFILE\.maestro\bin"
```

### 3.3 Instalación en macOS / Linux
```bash
curl -FsSL "https://get.maestro.mobile.dev" | bash
export PATH="$PATH:$HOME/.maestro/bin"
```

Verifica la instalación:
```bash
maestro --version
```

---

## 4. Configuración del Emulador Android y Entorno Local

### 4.1 Creación del Emulador Recomendado (AVD)
Para un rendimiento óptimo con .NET MAUI 9:
- **Dispositivo:** Pixel 7 o Pixel 8
- **Imagen del Sistema:** Android 14 (API 34) o Android 15 (API 35) con Google APIs (Arquitectura `x86_64`)
- **RAM:** 4096 MB | **Heap:** 512 MB

Iniciar emulador por terminal:
```bash
emulator -avd Pixel_7_API_34 -netdelay none -netspeed full -no-snapshot-load
```

### 4.2 Deshabilitar Animaciones del Sistema (Para Mayor Determinismo)
Las animaciones del sistema operativo pueden ralentizar las aserciones de UI Automator. Se recomienda deshabilitarlas en el dispositivo de pruebas:
```bash
adb shell settings put global window_animation_scale 0
adb shell settings put global transition_animation_scale 0
adb shell settings put global animator_duration_scale 0
```

### 4.3 Mapeo de Puertos de Red (Reverse Proxy)
Si el emulador necesita comunicarse con un backend local de desarrollo en el puerto 3000:
```bash
# Redirige el puerto 3000 del emulador al puerto 3000 de la máquina host
adb reverse tcp:3000 tcp:3000
```

---

## 5. Compilación e Instalación de CargoPulse Mobile

Antes de ejecutar las pruebas Maestro, compila e instala el paquete APK en el dispositivo conectado:

```bash
# 1. Compilación del APK para Android (Release o Debug)
dotnet publish src/CargoPulse.Mobile.csproj -f net9.0-android -c Release

# 2. Instalación en el emulador
adb install -r src/bin/Release/net9.0-android/com.cargopulse.mobile-Signed.apk

# 3. Validar que el paquete esté presente en el dispositivo
adb shell pm list packages | grep com.cargopulse.mobile
```

---

## 6. Ejecución de los Flujos de Prueba

### 6.1 Ejecutar la Suite Completa en Orden Secuencial
Para ejecutar todos los flujos mediante el orquestador principal:
```bash
maestro test .maestro/suite.yaml
```

O ejecutar todos los flujos de la carpeta de manera automática:
```bash
maestro test .maestro/flows/
```

### 6.2 Ejecutar Flujos Individuales
```bash
# Flujo 1: Autenticación, error de contraseña vacía y Dashboard
maestro test .maestro/flows/01_auth_flow.yaml

# Flujo 2: Navegación de escáner a búsqueda manual y detalle de embarque
maestro test .maestro/flows/02_manual_search_and_detail_flow.yaml

# Flujo 3: Formulario de inspección, discrepancia de sello, daño y firma táctil
maestro test .maestro/flows/03_inspection_and_signature_flow.yaml

# Flujo 4: Cola de sincronización SQLite y disparo de sync en segundo plano
maestro test .maestro/flows/04_offline_sync_flow.yaml
```

### 6.3 Uso de Maestro Studio (Inspección Visual Interactiva)
Maestro incluye una herramienta visual interactiva que se ejecuta en el navegador web para inspeccionar la jerarquía de UI, probar selectores y generar comandos en tiempo real:
```bash
maestro studio
```
Abre automáticamente `http://localhost:9999`, permitiendo hacer clic sobre cualquier elemento de la pantalla de CargoPulse Mobile y ver sus selectores idóneos.

### 6.4 Generación de Reportes JUnit y Artifacts
Para pipelines de integración continua o trazabilidad de calidad:
```bash
maestro test --format junit --output maestro-results.xml .maestro/suite.yaml
```

---

## 7. Detalle Técnico de los Flujos E2E

### Flujo 01: Autenticación & Validación RBAC (`01_auth_flow.yaml`)
1. **Lanzamiento Limpio:** Inicia la app con `clearState: true` para purgar tokens residuales en `SecureStorage`.
2. **Prueba Negativa:** Ingresa `operador@cargopulse.com`, omite la contraseña y pulsa `INICIAR SESIÓN`.
   - *Aserción:* Verifica la visualización del banner `"El correo y la contraseña son requeridos"`.
3. **Autenticación Válida:** Ingresa contraseña de operador y somete el formulario.
4. **Validación de Dashboard:**
   - *Aserción:* Badge de rol con texto `"LOGISTICS_OPERATOR"`.
   - *Aserción:* Tarjetas operativas: `"Escanear Contenedor"`, `"Cola Offline"`, `"Embarques Asignados"`.
   - *Aserción:* Barra de pestañas inferior (`Dashboard`, `Escáner`, `Embarques`, `Sincronización`).

### Flujo 02: Búsqueda Manual & Detalle de Embarque (`02_manual_search_and_detail_flow.yaml`)
1. **Precondición de Sesión:** Si la app está en Login, se autentica automáticamente.
2. **Navegación:** Desde el Dashboard pulsa `"ESCANEAR AHORA"` y transiciona a `"⌨ Búsqueda Manual"`.
3. **Búsqueda por Tracking:** Ingresa `'MX-TX-99881'` en el campo de texto y pulsa `"Buscar Embarque"`.
4. **Aserciones en Pantalla de Detalle:**
   - Banner de tracking: `"NÚMERO DE GUÍA / TRACKING"` con `'MX-TX-99881'`.
   - Tarjeta de Transportista: `"Transportista Asignado"`.
   - Botón de acción principal: `".*INICIAR INSPECCIÓN.*"`.
   - Timeline logístico: Realiza scroll y valida `"Etapas de la Ruta Logística"`.

### Flujo 03: Inspección Física, Discrepancia & Firma Digital (`03_inspection_and_signature_flow.yaml`)
1. **Entrada a Inspección:** Pulsa `"INICIAR INSPECCIÓN"` desde el detalle del embarque.
2. **Validación de Discrepancia Fiscal (Seal Mismatch):**
   - Ingresa un número de sello físico distinto al declarado en el manifiesto (`SEAL-MISMATCH-99881-DISCREPANT`).
   - *Aserción:* Aparición inmediata de la alerta `"¡DISCREPANCIA DE SELLO FISCAL!"` y `"El sello físico verificado difiere del sello declarado oficialmente."`.
3. **Reporte de Daños:**
   - Toca el botón de condición `"✕ DAÑADO"`.
   - *Aserción:* Expansión reactiva de `"DESCRIPCIÓN DE DAÑOS O ANOMALÍAS"`.
   - Ingresa descripción técnica del daño detectado.
4. **Firma Digital en DrawingPad:**
   - Escribe el nombre del transportista (`'Juan Chofer'`).
   - Verifica aviso de firma obligatoria (`"⚠ Firma táctil requerida antes de guardar"`).
   - Ejecuta gestos de `swipe` simulando el trazo táctil sobre el lienzo `DrawingView`.
   - Pulsa `"✓ Confirmar Firma"`.
   - *Aserción:* Estado actualizado a `"✓ Firma digital registrada correctamente"`.
5. **Guardado:** Pulsa `"GUARDAR INSPECCIÓN"` y confirma navegación de regreso al Dashboard.

### Flujo 04: Cola Offline & Sincronización en Segundo Plano (`04_offline_sync_flow.yaml`)
1. **Navegación a Sync:** Transiciona a la pestaña `//main/sync` (`"Cola de Sincronización"`).
2. **Aserciones de Cola:**
   - Estado de conectividad: Valida píldora dinámica (`"En Línea"` o `"Modo Offline"`).
   - Resumen de cola: `"ESTADO DE LA COLA"` y registros de inspección (`PENDING` / `SYNCED`).
3. **Disparo de Sincronización:** Pulsa `"SINCRONIZAR AHORA"`.
4. **Retroalimentación de Progreso:**
   - Valida mensajes o barra de progreso durante la sincronización.
   - Verifica que el botón y la vista se estabilicen al completar el ciclo.

---

## 8. Integración Continua (CI/CD Pipeline con GitHub Actions)

A continuación se presenta la receta completa de GitHub Actions para ejecutar el suite E2E de Maestro en cada Pull Request a `main`:

```yaml
name: CargoPulse Mobile - Maestro E2E Tests

on:
  pull_request:
    branches: [ "main" ]
  workflow_dispatch:

jobs:
  maestro-android-e2e:
    name: Android E2E Tests (Maestro CLI)
    runs-on: macos-14 # macOS runners ofrecen aceleración nativa de hardware para emuladores Android

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4

      - name: Setup .NET 9 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install .NET MAUI Android Workload
        run: dotnet workload install android

      - name: Setup Java 17
        uses: actions/setup-java@v4
        with:
          distribution: 'temurin'
          java-version: '17'

      - name: Build Android Release APK
        run: |
          dotnet publish src/CargoPulse.Mobile.csproj -f net9.0-android -c Release

      - name: Install Maestro CLI
        run: |
          curl -FsSL "https://get.maestro.mobile.dev" | bash
          echo "$HOME/.maestro/bin" >> $GITHUB_PATH

      - name: Run Maestro Tests on Android Emulator
        uses: reactivecircus/android-emulator-runner@v2
        with:
          api-level: 34
          target: google_apis
          arch: x86_64
          profile: pixel_7
          disable-animations: true
          script: |
            # 1. Instalar la aplicación compilada
            adb install -r src/bin/Release/net9.0-android/com.cargopulse.mobile-Signed.apk

            # 2. Configurar reverse proxy si hay mock server corriendo
            adb reverse tcp:3000 tcp:3000 || true

            # 3. Ejecutar suite E2E de Maestro
            maestro test --format junit --output maestro-results.xml .maestro/suite.yaml

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: maestro-junit-results
          path: maestro-results.xml

      - name: Upload Maestro Failure Artifacts & Screenshots
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: maestro-failure-logs
          path: ~/.maestro/tests/**/*
```

---

## 9. Buenas Prácticas y Resolución de Problemas (Troubleshooting)

### Selectores Robustos con Fallbacks
- Evite anclarse a coordenadas absolutas de pantalla (`point: 200, 400`), ya que varían entre densidades de pantalla (xxhdpi vs xxhdpi).
- Prefiera texto visible, etiquetas de accesibilidad (`AutomationId`) o expresiones regulares:
  ```yaml
  - tapOn:
      text: ".*INICIAR INSPECCIÓN.*"
  ```
- Para campos de entrada donde el teclado virtual cubra botones inferiores, utilice siempre el comando `hideKeyboard` tras ingresar texto:
  ```yaml
  - inputText: "Juan Chofer"
  - hideKeyboard
  ```

### Depuración de Errores Comunes

| Síntoma | Causa Frecuente | Solución |
| :--- | :--- | :--- |
| `Device not found or unauthorized` | `adb` perdió la conexión con el emulador | Ejecutar `adb kill-server && adb start-server` y verificar con `adb devices`. |
| Elemento fuera de la vista | La pantalla contiene scroll (`ScrollView`) | Usar `scrollUntilVisible` indicando `direction: DOWN`. |
| El teclado oculta botones de acción | Teclado de software activo tras `inputText` | Agregar `- hideKeyboard` inmediatamente después de cada entrada. |
| Firma no registrada | El trazo de swipe no tocó la superficie del `DrawingView` | Ajustar coordenadas porcentuales (ej. `start: 30%, 75%`, `end: 70%, 75%`) y pulsar `"✓ Confirmar Firma"`. |
