# 04. Appflow & Navigation State Machines
## CargoPulse Mobile - Field Operations Companion

> **Documento:** `docs/specs/04_APPFLOW.md`  
> **Versión:** 1.0.0  
> **Estado:** En Revisión / Especificación Formal  
> **Fecha:** Octubre 2026  
> **Autor:** Arquitecto Técnico Líder  
> **Proyecto:** CargoPulse Mobile (`CargoPulse-mobile`)  
> **Constitución de Referencia:** `MASTER_ENGINEERING_CONSTITUTION.md` (v2.1)

---

## 1. Árbol de Navegación Jerárquico (Shell Navigation)

La navegación móvil se implementa mediante **.NET MAUI Shell**, permitiendo rutas fuertemente tipadas y transiciones limpias:

```mermaid
flowchart TD
    Splash["SplashScreen / AppStartup"] --> CheckAuth{"¿Token válido en SecureStorage?"}
    
    CheckAuth -->|No / Expirado| LoginRoute["/login (LoginPage)"]
    CheckAuth -->|Sí| MainShell["//main (AppShell)"]

    LoginRoute -->|Login exitoso| MainShell

    subgraph AppShell["Estructura Principal (Tab / Flyout)"]
        TabDashboard["Tab 1: /dashboard (DashboardPage)"]
        TabScanner["Tab 2: /scanner (ScannerPage)"]
        TabShipments["Tab 3: /shipments (ShipmentsListPage)"]
        TabSync["Tab 4: /sync (SyncQueuePage)"]
        TabProfile["Tab 5: /profile (ProfileSettingsPage)"]
    end

    MainShell --> TabDashboard
    MainShell --> TabScanner
    MainShell --> TabShipments
    MainShell --> TabSync
    MainShell --> TabProfile

    subgraph ModalesYDetalles["Rutas Secundarias y Wizards"]
        ManualSearch["/scanner/manual (ManualEntryModal)"]
        ShipmentDetail["/shipment/detail?tracking={id} (ShipmentDetailPage)"]
        InspectionWizard["/shipment/inspect?id={id} (InspectionPage)"]
        CameraCapture["/inspection/camera (NativeCameraCapture)"]
        SignatureCanvas["/inspection/signature (SignatureModal)"]
        AddNoteModal["/shipment/notes/add (AddNoteModal)"]
    end

    TabScanner -->|Código detectado| ShipmentDetail
    TabScanner -->|Opción manual| ManualSearch
    ManualSearch -->|Guía encontrada| ShipmentDetail
    
    TabShipments -->|Seleccionar item| ShipmentDetail
    TabDashboard -->|Acceso rápido| TabScanner
    TabDashboard -->|Ver pendientes| TabSync

    ShipmentDetail -->|Botón Iniciar Inspección| InspectionWizard
    ShipmentDetail -->|Botón Bitácora| AddNoteModal

    InspectionWizard -->|Captura de Evidencia| CameraCapture
    CameraCapture -->|Foto guardada| InspectionWizard
    InspectionWizard -->|Firma Chofer| SignatureCanvas
    SignatureCanvas -->|Firma confirmada| InspectionWizard
    InspectionWizard -->|Finalizar y Guardar| TabSync
```

---

## 2. Matriz de Control de Acceso por Pantalla (RBAC)

Los permisos en la aplicación móvil se gobiernan por el campo `role` contenido en el JWT y resguardado en `SecureStorage`:

| Pantalla / Ruta | `LOGISTICS_OPERATOR` (Inspector / Chofer) | `FREIGHT_AUDITOR` (Agente / Auditor) | `ADMIN` (Supervisor) | Comportamiento si no autorizado |
| :--- | :---: | :---: | :---: | :--- |
| **`/login`** | Permitido | Permitido | Permitido | N/A |
| **`/dashboard`** | Permitido | Permitido | Permitido | Acceso estándar |
| **`/scanner`** | Permitido | Permitido | Permitido | Acceso estándar |
| **`/shipment/detail`** | Permitido | Permitido | Permitido | Acceso estándar |
| **`/shipment/inspect` (Inspección & Firma)** | **Permitido (Lectura/Escritura)** | Solo Lectura | **Permitido (Total)** | Deshabilita inputs de edición para `FREIGHT_AUDITOR`. |
| **`/shipment/notes/add`** | Permitido | Permitido | Permitido | Acceso estándar |
| **`/alerts/resolve` (Cierre de alertas)** | Bloqueado | **Permitido** | **Permitido** | Oculta botón de resolución a Operadores. |
| **`/sync` (Cola y forzado de sync)** | Permitido | Permitido | Permitido | Operadores pueden forzar reintento de sus envíos. |
| **`/settings/admin`** | Bloqueado | Bloqueado | **Permitido** | Redirige a `/dashboard` con mensaje "Acceso restringido". |

---

## 3. Máquinas de Estados de Flujos Críticos

### 3.1 Flujo de Autenticación y Ciclo de Sesión
```mermaid
stateDiagram-v2
    [*] --> VerificandoAlmacenamiento: Arranque de App
    VerificandoAlmacenamiento --> SesionActiva: Token existe y fecha vigente
    VerificandoAlmacenamiento --> NoAutenticado: No hay token o está expirado

    NoAutenticado --> Autenticando: Usuario envía credenciales (POST /api/auth/login)
    Autenticando --> NoAutenticado: Error 401 Credenciales incorrectas
    Autenticando --> SesionActiva: Token 200 OK -> Guardar en SecureStorage

    SesionActiva --> SesionCerrada: Usuario pulsa "Cerrar Sesión"
    SesionCerrada --> NoAutenticado: SecureStorage purgado -> Redirigir a /login
    SesionActiva --> NoAutenticado: Respuesta 401 Unauthorized en llamada de red
```

### 3.2 Flujo Operativo de Escaneo, Inspección y Firma Digital
```mermaid
stateDiagram-v2
    [*] --> EsperandoEscaneo: Pantalla Scanner Activa
    EsperandoEscaneo --> Decodificando: Código dentro del visor
    Decodificando --> BuscandoEmbarque: Código válido decodificado
    Decodificando --> EsperandoEscaneo: Error de lectura / timeout

    BuscandoEmbarque --> DetalleEmbarque: Encontrado en SQLite o API
    BuscandoEmbarque --> EsperandoEscaneo: Guía no encontrada (Mostrar alerta)

    DetalleEmbarque --> FormularioInspeccion: Tap "Nueva Inspección"
    
    state FormularioInspeccion {
        [*] --> VerificandoSellos: Captura número de sello físico
        VerificandoSellos --> EvaluandoDaños: Sello coincide o marcado como violado
        EvaluandoDaños --> CapturandoFotos: Si hay daños -> Abre cámara nativa
        CapturandoFotos --> EvaluandoDaños: Foto comprimida y asociada
        EvaluandoDaños --> CapturandoFirma: Tap "Firmar Liberación"
        CapturandoFirma --> InspeccionLista: Firma trazada y confirmada
    }

    FormularioInspeccion --> GuardadoLocal: Tap "Finalizar Inspección"
    GuardadoLocal --> ColaSincronizacion: Registro escrito en SQLite (Status = PENDING_SYNC)
    ColaSincronizacion --> [*]: Retorna al Dashboard con éxito háptico
```

### 3.3 Flujo del Motor de Sincronización en Segundo Plano (Offline Sync Engine)
```mermaid
stateDiagram-v2
    [*] --> EnReposo: Esperando registros o evento de red

    EnReposo --> VerificandoConexion: Nuevo registro local creado O ConnectivityChanged
    
    state VerificandoConexion {
        [*] --> ChequeoPing: Connectivity.NetworkAccess == Internet
        ChequeoPing --> HayInternet: Ping exitoso a /api
        ChequeoPing --> SinInternet: Fallo de socket / No Internet
    }

    SinInternet --> EnReposo: Mantener status PENDING_SYNC
    HayInternet --> ProcesandoCola: Leer registros WHERE SyncStatus = 'PENDING'

    state ProcesandoCola {
        [*] --> TomandoSiguienteItem: Obtener inspección pendiente más antigua
        TomandoSiguienteItem --> SubiendoPayload: POST /api/inspections (JSON + Firma + Fotos)
        SubiendoPayload --> ActualizandoLocalExito: HTTP 201 Created
        ActualizandoLocalExito --> HayMasItems: Marcar SyncStatus = 'SYNCED'
        SubiendoPayload --> RegistrandoFallo: HTTP 5xx / Timeout de red
        RegistrandoFallo --> EnReposo: Incrementar reintentos (Backoff)
    }

    HayMasItems --> TomandoSiguienteItem: Sí hay más
    HayMasItems --> EnReposo: No hay más (Contador pendientes = 0)
```
