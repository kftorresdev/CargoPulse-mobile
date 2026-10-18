# 06. Dynamic Implementation Plan & Work Breakdown Structure (WBS)
## CargoPulse Mobile - Field Operations Companion

> **Documento:** `docs/specs/06_IMPLEMENTATION_PLAN.md`  
> **Versión:** 1.0.0  
> **Estado:** En Revisión / Especificación Formal  
> **Fecha:** Octubre 2026  
> **Autor:** Arquitecto Técnico Líder  
> **Proyecto:** CargoPulse Mobile (`CargoPulse-mobile`)  
> **Constitución de Referencia:** `MASTER_ENGINEERING_CONSTITUTION.md` (v2.1)

---

## 1. Metodología de Ejecución Dinámica

Conforme a la Sección 10 y Sección 11 de la Constitución, el plan de trabajo se estructura en un ciclo de vida ágil dinámico dividido en **Sprint 0 a Sprint 5**, diseñado para garantizar la entrega de valor funcional continuo con base en el estándar **Polyrepo** y respetando rigurosamente la regla **Backend-First** mediante la API NestJS ya operativa.

### 1.1 Gobernanza de Ejecución con Enjambre de Subagentes (Multi-Agent Swarm Standard)
Para prevenir la fatiga cognitiva, degradación de contexto y errores por sobrecarga en un único agente, se establece como regla inquebrantable para cada Sprint:
- **Evaluación de Envergadura Mandatoria:** Al inicio de cada Sprint, el Agente Principal evaluará la complejidad del alcance.
- **Despliegue Elástico Sin Límites:** Si el Sprint abarca múltiples capas (UI, DB local, API client, QA), se desplegará el número necesario de subagentes especializados (2, 4, 6 o más) en paralelo o secuencia.
- **Especialización por Capa:** Subagentes dedicados a Data/SQLite, Network/Sync, UI XAML/Hardware, QA Unit Testing y Auditoría de Código Limpio.
- **Orquestación Centralizada:** El Agente Principal actúa como Arquitecto Técnico Líder, coordinando la integración armónica, compilación y certificación de la Definition of Done (DoD).

---

## 2. Work Breakdown Structure (WBS) Dinámico por Sprints

### 2.1 Sprint 0: Fundación Técnica & Scaffolding .NET MAUI
- **Objetivo del Sprint:** Inicializar el proyecto .NET MAUI 9 en C#, configurar la arquitectura limpia de 3 capas, instalar los paquetes oficiales NuGet y preparar el pipeline de CI/CD en GitHub Actions.
- **Entregables:**
  - Proyecto `CargoPulse.Mobile.csproj` inicializado con target `net9.0-android` (y soporte `net9.0-ios`).
  - Instalación de paquetes NuGet aprobados:
    - `CommunityToolkit.Mvvm` (8.3+)
    - `CommunityToolkit.Maui` (9.0+)
    - `sqlite-net-pcl` (1.9.141) y `SQLitePCLRaw.bundle_green`
    - `ZXing.Net.Maui.Controls` (0.4+)
  - Configuración del árbol de carpetas de 3 capas según Constitución (Sec. 5.3):
    ```text
    src/
    ├── Core/         # Converters XAML, Handlers, Helpers, Constantes de Conectividad
    ├── Data/         # SQLiteContext, Repositorios locales, ApiClients, DTOs
    ├── ViewModels/   # ViewModels reactivos con [ObservableProperty] y [RelayCommand]
    └── Views/        # Páginas XAML limpias (Code-behind sin lógica de negocio)
    ```
  - Archivo `.github/workflows/mobile-ci.yml` configurado para validación de compilación y pruebas unitarias.

---

### 2.2 Sprint 1: Autenticación Segura, Shell Navigation & Dashboard
- **Objetivo del Sprint:** Implementar el flujo de acceso seguro consumiendo `POST /api/auth/login`, gestión de credenciales en `SecureStorage`, interceptor HTTP de Bearer token y el Dashboard operativo principal.
- **Entregables:**
  - `AuthApiClient` y `AuthService` con métodos `LoginAsync(email, password)` y `LogoutAsync()`.
  - Almacenamiento seguro del token JWT, expiración y rol en `SecureStorage.Default`.
  - `AppShell.xaml` con enrutamiento declarativo fuertemente tipado.
  - `LoginPage.xaml` y `LoginViewModel` con validación de entradas y feedback de carga.
  - `DashboardPage.xaml` y `DashboardViewModel` mostrando datos del operador, métricas rápidas de turno y accesos directos al escáner y la cola de sincronización.

---

### 2.3 Sprint 2: Escáner Óptico de Cámara & Detalle del Embarque
- **Objetivo del Sprint:** Construir el módulo de escaneo de códigos de barra (Code 128) y QR por cámara física con linterna y consulta inmediata del detalle del embarque.
- **Entregables:**
  - `ScannerPage.xaml` con control `CameraBarcodeReaderView` de ZXing y overlay visual industrial.
  - Manejo de permisos nativos de cámara en Android/iOS en tiempo de ejecución.
  - Retroalimentación háptica (vibración) tras detección de código válido.
  - Modal de entrada manual de guía de contingencia (`ManualEntryModal`).
  - `ShipmentApiClient` para consultar `GET /api/shipments/:id` o búsqueda por tracking.
  - `ShipmentDetailPage.xaml` que proyecta la información del contenedor, transportista, etapas de flete y alertas vigentes.

---

### 2.4 Sprint 3: Módulo de Inspección, Captura Fotográfica & Firma Táctil
- **Objetivo del Sprint:** Desarrollar el formulario de inspección física en patio (*Proof of Condition*), toma de fotografías de evidencia con cámara y recolección de firma táctil digital del chofer.
- **Entregables:**
  - `InspectionPage.xaml` y `InspectionViewModel` con formulario de sellos fiscales y condición de carrocería.
  - Integración de `MediaPicker.Default.CapturePhotoAsync()` para captura de hasta 3 fotografías con redimensionamiento y compresión JPEG local.
  - `DrawingView` de `CommunityToolkit.Maui` para captura de trazos de firma del transportista con exportación a PNG Base64.
  - Generación de UUID v4 offline para la inspección y vinculación atómica con el `shipmentId`.

---

### 2.5 Sprint 4: Motor Offline-First con SQLite & Cola de Sincronización
- **Objetivo del Sprint:** Implementar la persistencia local de inspecciones y el motor de sincronización en segundo plano con reintentos automáticos ante eventos de red.
- **Entregables:**
  - `LocalInspectionDatabase` con `sqlite-net-pcl` creando tabla `Inspections` local en sandbox seguro.
  - Guardado transparente inmediato: si no hay red, la inspección se sella con `SyncStatus = PENDING`.
  - `BackgroundSyncEngine` suscrito al evento `Connectivity.ConnectivityChanged` y disparador periódico.
  - Envío por lotes secuenciales a `POST /api/inspections` con backoff exponencial.
  - `SyncQueuePage.xaml` mostrando lista de registros pendientes, botón de forzar sincronización y barra de progreso.

---

### 2.6 Sprint 5: Hardening, Pruebas Unitarias, Maestro E2E & Release
- **Objetivo del Sprint:** Aseguramiento de calidad, testing unitario de ViewModels, automatización de pruebas de interfaz con Maestro y empaquetado release APK firmado.
- **Entregables:**
  - Suite de pruebas unitarias en `test/CargoPulse.Mobile.Tests` con **xUnit + FluentAssertions + Moq** (sin necesidad de emulador, conforme a Constitución Sec. 8.2).
  - Flujo de prueba E2E declarativo con **Maestro** (`.maestro/flow_inspection.yaml`) validando el recorrido completo de login, escaneo ficticio e inspección.
  - Auditoría de consumo de memoria y cierre seguro de sockets/streams de imágenes.
  - Generación de build Release firmada con ProGuard/R8 habilitado para optimización de tamaño de binario.

---

## 3. Matriz de Dependencias Críticas y Gestión de Riesgos

| Riesgo Técnico / Operativo | Impacto | Probabilidad | Plan de Mitigación y Contingencia |
| :--- | :---: | :---: | :--- |
| **Pérdida de señal durante la toma de fotos o firma en patio** | Alto | Alta | **Arquitectura Offline-First:** Las imágenes se escriben primero en disco local y la base SQLite antes de intentar cualquier conexión HTTP. Cero pérdida de datos. |
| **Códigos de barras sucios, rayados o con mala iluminación** | Medio | Alta | Integración de botón de linterna/flash en el visor y opción inmediata de "Búsqueda Manual por Número de Guía". |
| **Imágenes de alta resolución (12-48 MP) saturando memoria y payload de red** | Alto | Media | Pipeline de pre-procesamiento que redimensiona forzosamente la imagen a max 1920x1080 con compresión JPEG al 80% antes de serializar. |
| **Expiración de JWT mientras el operador trabaja desconectado** | Medio | Media | Detección de token local: las inspecciones se firman y resguardan en SQLite con el `inspectorId` registrado localmente; al reconectar, si el token expiró, se solicita re-autenticación rápida sin borrar la cola de SQLite. |
| **Diferencias de red en Emulador Android vs Dispositivo Real** | Bajo | Alta | Abstracción de URL base en configuración (`10.0.2.2` para emulador, IP de LAN para dispositivo físico, `localhost` para Windows). |

---

## 4. Definition of Done (DoD) Inquebrantable

Para dar por concluido cualquier Sprint, cada tarea o User Story debe satisfacer sin excepción los siguientes criterios:

1. **Zero-Code Violation:** Todo archivo de código fuente cumple la regla de tres capas; ningún archivo `.xaml.cs` contiene lógica de negocio o llamadas HTTP directas.
2. **Clean Code & Tipado Estricto:** Código compilado con cero warnings, cero uso de `dynamic` y apego estricto a guard clauses (sin pirámides de `if`).
3. **Persistencia & Manejo de Estados:** La pantalla implementa los 4 estados obligatorios (Loading, Empty, Error, Success).
4. **Resiliencia Offline Verificada:** La funcionalidad fue probada desconectando intencionalmente la interfaz de red (Modo Avión / Network Off) para comprobar que la app no colapsa y almacena el estado localmente.
5. **Pruebas Automatizadas:** Todo ViewModel cuenta con su correspondiente prueba unitaria en xUnit con cobertura de casos de éxito y de borde.
6. **Commits Semánticos:** Todos los commits del sprint siguen la convención Conventional Commits (`feat:`, `fix:`, `chore:`, etc.).
