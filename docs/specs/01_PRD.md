# 01. Product Requirements Document (PRD)
## CargoPulse Mobile - Field Operations Companion

> **Documento:** `docs/specs/01_PRD.md`  
> **Versión:** 1.0.0  
> **Estado:** En Revisión / Especificación Formal  
> **Fecha:** Octubre 2026  
> **Autor:** Arquitecto Técnico Líder  
> **Proyecto:** CargoPulse Mobile (`CargoPulse-mobile`)  
> **Constitución de Referencia:** `MASTER_ENGINEERING_CONSTITUTION.md` (v2.1)

---

## 1. Problema de Negocio y Oportunidad

### 1.1 Contexto Operativo
En el corredor logístico transfronterizo México - Estados Unidos (ej. aduanas de Nuevo Laredo / Laredo, Tijuana / Otay Mesa, Cd. Juárez / El Paso), las terminales de patio y cruce internacional gestionan miles de cajas de tráiler y contenedores intermodales diariamente. 

Actualmente, los inspectores de patio y choferes enfrentan graves fricciones:
1. **Zonas Muertas de Cobertura Celular:** En los patios aduanales y muelles de maniobras, la señal 4G/5G y Wi-Fi es intermitente o inexistente. Las soluciones web estándar fallan al perder conexión, impidiendo registrar o validar cargas.
2. **Registro Manual y Retrasos de Transcripción:** Validaciones de sellos de seguridad y números de contenedor registradas en papel o planillas manuales provocan demoras de hasta 45 minutos por camión en garita y errores tipográficos en el número de guía/tracking.
3. **Disputas de Responsabilidad por Daños (Proof of Condition Cero):** Cuando un embarque llega con sellos rotos, abolladuras o carga vulnerada en destino final, las aseguradoras y transportistas entran en litigios costosos debido a la falta de evidencia fotográfica geo-fechada y firmas digitales táctiles capturadas al momento exacto de entrega/recepción.

### 1.2 Declaración de Visión del Producto
**CargoPulse Mobile** es la aplicación móvil de campo oficial (Field Operations Companion) diseñada para inspectores de patio, choferes y agentes aduanales. Proporciona escaneo óptico instantáneo de guías y contenedores, ejecución de listas de verificación de inspección con captura fotográfica de daños, recolección de firmas táctiles digitales y operación **100% Offline-First**, garantizando que ninguna operación se detenga por falta de red y sincronizando automáticamente con el backend central de CargoPulse al recuperar conectividad.

---

## 2. Público Objetivo (User Personas) y Roles de Acceso

El sistema de roles se mapea estrictamente con el backend existente de CargoPulse (`UserRole` en JWT / PostgreSQL):

| Rol Backend (`Role`) | Perfil de Campo (User Persona) | Responsabilidades en la App Móvil | Dispositivos Habituales |
| :--- | :--- | :--- | :--- |
| **`LOGISTICS_OPERATOR`** | **Inspector de Patio / Muelle** | - Escanear código de barra/QR de contenedores y remolques.<br>- Realizar inspección visual de sellos y estado exterior.<br>- Tomar fotografías de evidencia ante anomalías.<br>- Solicitar y registrar firma táctil del transportista. | Smartphones Android/iOS industriales o rugerizados (Zebra, Honeywell, Samsung Galaxy XCover). |
| **`LOGISTICS_OPERATOR`** | **Chofer / Transportista** | - Visualizar guía de embarque digital asignada.<br>- Confirmar recepción/entrega de unidad.<br>- Plasmar su firma táctil de liberación en la pantalla del inspector. | Smartphone personal del chofer o terminal de mano del patio. |
| **`FREIGHT_AUDITOR`** | **Agente Aduanal / Auditor de Carga** | - Consultar bitácora de eventos y sellos históricos.<br>- Auditar discrepancias entre el manifiesto físico y los datos aduanales.<br>- Validar la resolución de alertas operativas críticas. | Tablets (iPad / Android Tablet) o Smartphones de alta gama. |
| **`ADMIN`** | **Supervisor de Terminal** | - Acceso total: anulación o re-inspección de embarques.<br>- Monitoreo de cola de sincronización de todas las cuadrillas. | Terminal móvil o estación de trabajo de oficina. |

---

## 3. Alcance Funcional: In-Scope vs. Out-of-Scope (v1.0)

### 3.1 Dentro del Alcance (In-Scope - Versión 1.0)
- **Módulo 1: Autenticación Segura y Perfil:**
  - Login mediante correo electrónico y contraseña contra `POST /api/auth/login`.
  - Almacenamiento seguro del token JWT en `SecureStorage` del dispositivo.
  - Cierre de sesión seguro y purga de credenciales temporales.
  - Detección de rol y expiración de token.
- **Módulo 2: Escáner Óptico de Campo (Cámara):**
  - Lector de códigos de barra (Code 128, Code 39) y 2D (QR Code, DataMatrix) integrado mediante cámara nativa.
  - Soporte de linterna/flash para inspecciones nocturnas en patio.
  - Búsqueda y precarga automática de la información del embarque (`Shipment`) tras escaneo exitoso.
- **Módulo 3: Inspección de Estado y Proof of Condition:**
  - Formulario estructurado de inspección de unidad:
    - Estado general del contenedor (Excelente, Regular, Dañado).
    - Estado del sello fiscal/seguridad (Intacto, Roto, Discrepante con manifiesto).
    - Captura de número de sello leído.
  - Captura fotográfica directa desde la cámara del dispositivo con compresión local optimizada.
  - Captura de firma táctil digital del chofer/receptor (Draw Canvas con exportación a imagen / base64).
  - Marcado de severidad y generación de alerta operativa en caso de anomalía grave.
- **Módulo 4: Arquitectura Offline-First y Motor de Sincronización:**
  - Base de datos local SQLite (`sqlite-net-pcl`) cifrada/aislada en sandbox de la app.
  - Guardado local instantáneo de toda inspección generada, permitiendo continuar operando sin red.
  - Monitoreo reactivo de conectividad (`IConnectivity` / `Connectivity.Current`).
  - Cola de sincronización (`SyncQueue`) en segundo plano que sube automáticamente inspecciones, fotos y firmas pendientes al restablecerse la red.
  - Indicador visual permanente del estado de conexión (En línea / Fuera de línea) y contador de ítems pendientes por sincronizar.
- **Módulo 5: Consulta y Bitácora Rápida:**
  - Visualización del resumen de etapas del flete (`FreightStages`) y alertas abiertas.
  - Adición rápida de notas operativas de campo a la bitácora existente del embarque (`/api/collaboration/shipment/:id/notes`).

### 3.2 Fuera del Alcance (Out-of-Scope - Versiones Futuras)
- Pasarelas de cobro o liquidación de fletes en la app móvil (esto se gestiona en CargoPulse Web).
- Generación de facturas electrónicas CFDI / Carta Porte SAT (delegado al backend/web).
- Rastreo satelital GPS en tiempo real 24/7 en segundo plano con consumo continuo de batería (se registra solo geolocalización puntual del evento de inspección).
- Edición estructural de etapas de contratos o tarifas (exclusivo de perfiles financieros en Web).

---

## 4. Historias de Usuario y Criterios de Aceptación (Gherkin)

### HU-01: Autenticación Segura y Persistencia de Sesión
```gherkin
Característica: Autenticación y resguardo de sesión de operador

  Criterio 1.1: Inicio de sesión exitoso con conectividad
    Dado que el inspector abre la app CargoPulse Mobile y tiene conexión a red
    Cuando ingresa su email válido y su contraseña registrada
    Y presiona el botón "Iniciar Sesión"
    Entonces el sistema envía la solicitud a POST /api/auth/login
    Y al recibir una respuesta exitosa HTTP 200 con el JWT y datos del usuario
    Y guarda el token de manera segura en SecureStorage
    Y navega al Dashboard Operativo de Campo mostrando su nombre y rol.

  Criterio 1.2: Fallo de autenticación
    Dado que el usuario ingresa credenciales inválidas
    Cuando presiona "Iniciar Sesión"
    Entonces el sistema muestra un mensaje de error claro "Credenciales incorrectas"
    Y no almacena ningún token en SecureStorage.

  Criterio 1.3: Sesión persistente al reabrir la app
    Dado que el operador ya inició sesión previamente y su JWT sigue vigente
    Cuando reabre la app tras cerrarla o reiniciar el teléfono
    Entonces el sistema verifica la validez local del token
    Y omite la pantalla de Login, ingresando directamente al Dashboard.
```

### HU-02: Escaneo Rápido de Contenedor o Guía de Transporte
```gherkin
Característica: Reconocimiento óptico de contenedor o guía de embarque

  Criterio 2.1: Escaneo exitoso de código QR o Code 128
    Dado que el inspector se encuentra frente a un contenedor en patio
    Y presiona el botón de acceso rápido "Escanear Contenedor"
    Cuando la cámara enfoca el código del contenedor (ej. "CP-789012" o formato ISO 6346)
    Entonces el lector decodifica la cadena en menos de 2 segundos con retroalimentación háptica (vibración)
    Y consulta localmente en SQLite o en GET /api/shipments el detalle del embarque
    Y transiciona automáticamente a la pantalla "Detalle del Embarque".

  Criterio 2.2: Búsqueda manual de respaldo
    Dado que la etiqueta física del contenedor está rasgada o ilegible por suciedad
    Cuando el inspector pulsa "Entrada Manual"
    Y teclea los 8 a 12 caracteres del número de tracking
    Entonces el sistema valida el formato y carga la ficha técnica correspondiente.
```

### HU-03: Registro de Inspección de Estado y Proof of Condition
```gherkin
Característica: Captura de reporte de daños, sellos y firma de liberación

  Criterio 3.1: Inspección estándar sin discrepancias
    Dado que el inspector está en la ficha del embarque seleccionado
    Cuando completa el checklist indicando "Sello Intacto", número de sello coincidente y "Contenedor en Buen Estado"
    Y solicita la firma del transportista en el pad táctil
    Y presiona "Finalizar y Guardar Inspección"
    Entonces la inspección se empaqueta con ID único (UUID v4), estampa de tiempo ISO y coordenadas
    Y se almacena inmediatamente en la base de datos local SQLite.

  Criterio 3.2: Reporte de daños con captura de evidencia fotográfica
    Dado que el inspector detecta un golpe estructural o sello roto
    Cuando marca la casilla "Sello Roto / Violado" o "Daño Estructural"
    Y pulsa "Tomar Evidencia Fotográfica" abriendo la cámara del sistema
    Y captura hasta 3 fotografías claras del daño
    Entonces la app comprime las imágenes a formato JPEG optimizado (resolución máxima 1920x1080)
    Y asocia las fotografías al registro de inspección local
    Y genera una alerta de severidad "HIGH" o "CRITICAL" ligada al embarque.
```

### HU-04: Resiliencia Offline y Sincronización Automática
```gherkin
Característica: Operación continua sin internet y sincronización en segundo plano

  Criterio 4.1: Guardado transparente sin conectividad
    Dado que el inspector se encuentra en una zona sin cobertura celular en el patio
    Cuando completa una inspección con fotografías y firma táctil
    Y presiona "Guardar Inspección"
    Entonces el sistema almacena la transacción en SQLite con status "PENDING_SYNC"
    Y muestra una confirmación visual "Guardado localmente. Se sincronizará al detectar conexión"
    Y el contador de pendientes de la barra de estado se incrementa en +1.

  Criterio 4.2: Sincronización automática reactiva
    Dado que existen 3 inspecciones locales con status "PENDING_SYNC"
    Cuando el dispositivo detecta que Connectivity.Current restablece acceso a internet
    Entonces el motor de sincronización inicia la subida secuencial a POST /api/inspections
    Y una vez que el backend confirma HTTP 201 Created para cada registro
    Y la app actualiza el status local a "SYNCED"
    Y decrementa el contador de pendientes hasta llegar a 0.
```

---

## 5. Métricas Clave de Éxito y Rendimiento (KPIs)

1. **Velocidad de Escaneo Óptico (Time to Scan):**  
   - $\le 1.5\text{ segundos}$ desde que el código entra en el visor de cámara hasta la decodificación háptica.
2. **Latencia de Guardado Local (Offline Write):**  
   - $\le 250\text{ ms}$ para persistir el registro de inspección, firma y rutas de imágenes en SQLite local.
3. **Tasa de Pérdida de Datos:**  
   - **0% de pérdida de inspecciones.** Ningún reporte de campo debe perderse por corte imprevisto de energía o cierre súbito de la app (*Zero Data Loss Guarantee*).
4. **Tiempo de Ciclo de Inspección Completa:**  
   - Reducción del tiempo promedio de inspección y liberación en patio de 15 minutos (papel) a $\le 3\text{ minutos}$ en digital.
5. **Consumo y Rendimiento:**  
   - Tamaño de paquete binario (APK / Release) $\le 45\text{ MB}$.
   - Arranque en frío (*Cold Start*) $\le 2.0\text{ segundos}$ en dispositivos de gama media.
   - Compresión fotográfica automática para reducir payload a $\le 600\text{ KB}$ por imagen sin pérdida de legibilidad de sellos.
