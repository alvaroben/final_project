# Guía de capturas en Postman — Evidencia de funcionamiento de la API

Proyecto final INF-4318 · API REST — Gestión de Citas de Consultorio Dental

Esta guía indica **exactamente qué peticiones ejecutar, con qué datos, qué respuesta debe
aparecer y qué demuestra cada captura**. Al terminar tendrá evidencia de los **51 endpoints**
de la API más las respuestas de validación y error.

> ### ⚡ No arme las peticiones a mano
>
> En [`docs/postman/`](postman/) están la colección y el environment ya construidos, con los
> **79 requests** de esta guía: cuerpos escritos, Bearer heredado, scripts que encadenan los IDs
> y un test de estado por petición. **Verificados contra la API: 79/79 devuelven el código
> esperado.** Vea la sección [1.3](#13-importar-la-colección-y-el-environment) para importarlos.
>
> De esos 79, dos son pasos de apoyo que no necesitan captura (`A06a` y `D02b`): quedan
> **77 capturas**.

---

## 1. Preparación del entorno

### 1.1 Levantar la API

```bash
docker compose up -d                              # SQL Server en localhost:1433
dotnet run --project src/ConsultorioDental.API    # API en http://localhost:5080
```

> **Recomendación:** haga las capturas sobre una **base de datos recién creada**. Los IDs que
> aparecen en esta guía (paciente 1, dentista 1, cita 1…) corresponden a la carga inicial
> automática. Para reiniciar: `docker compose down -v && docker compose up -d` y vuelva a
> ejecutar la API.

Use siempre `http://localhost:5080` (HTTP). Si usa `https://localhost:7101`, desactive antes
en Postman: **Settings → General → SSL certificate verification = OFF**, porque el certificado
de desarrollo es autofirmado y Postman rechaza la conexión.

### 1.2 Datos precargados (los usará en casi todas las peticiones)

| Entidad | IDs | Contenido |
|---|---|---|
| Usuario | 1 | `admin` / `Admin123*` — rol Administrador |
| Especialidades | 1–5 | Odontología General, Ortodoncia, Endodoncia, Periodoncia, Odontopediatría |
| Motivos | 1–5 | Dolor dental, Limpieza de rutina, Revisión de ortodoncia, Urgencia por trauma, Evaluación inicial |
| Servicios | 1–6 | Consulta general (RD$1,200 / 30 min), Limpieza dental (2,500 / 45), Extracción simple (3,500 / 45), Tratamiento de conducto (9,500 / 90), Resina estética (3,000 / 60), Ajuste de brackets (2,000 / 30) |
| Consultorios | 1–3 | C-01, C-02, C-03 |
| Dentistas | 1–3 | Carolina Méndez (esp. 1), Rafael Peña (esp. 2), Isabel Guzmán (esp. 3) |
| Pacientes | 1–4 | Juan Pérez, María Rodríguez, Luis Fortuna, Ana Santos |
| Horarios | 1–33 | Cada dentista: lunes a viernes 08:00–12:00 y 14:00–18:00; sábado 08:00–13:00 |
| Citas | 1, 2, 3 | #1 finalizada (hace 3 días) · #2 en proceso · #3 vigente (dentro de 2 días) |

Dos advertencias sobre las citas de ejemplo:

- La cita **#2 (En proceso)** solo está realmente "en proceso" durante los **45 minutos
  siguientes** a la creación de la base de datos. Si quiere capturar el estado `EnProceso`,
  tome esa captura al inicio de la sesión.
- Los dentistas **no atienden domingos**. Toda cita que agregue debe caer de lunes a sábado y
  dentro de 08:00–12:00 o 14:00–18:00 (sábado hasta 13:00).

### 1.3 Importar la colección y el environment

En Postman: **Import** → arrastre los dos archivos de [`docs/postman/`](postman/):

| Archivo | Qué trae |
|---|---|
| `ConsultorioDental.postman_collection.json` | Las 79 peticiones en 6 carpetas (A–F), con cuerpos, descripciones y tests |
| `ConsultorioDental.postman_environment.json` | Las 13 variables (`baseUrl`, `token`, los IDs de demo…) |

⚠️ **Seleccione el environment** `Consultorio Dental — Local` en el desplegable de la esquina
superior derecha. Si `{{baseUrl}}` aparece en rojo, es que no está seleccionado y la captura
saldrá con error.

Todo lo demás ya viene resuelto:

- **Autenticación heredada:** la colección tiene `Bearer {{token}}` a nivel raíz; las peticiones
  de 401/403 llevan su propia excepción configurada.
- **Token automático:** `A01` guarda el JWT en `{{token}}`; `A06a` guarda el del recepcionista
  en `{{tokenRecepcion}}`.
- **IDs encadenados:** cada `POST` de creación guarda el `id` devuelto (`{{pacienteDemoId}}`,
  `{{citaDemoId}}`…), que las peticiones de actualización y borrado consumen. **No hay que
  copiar ningún número a mano.**
- **Fecha siempre válida:** un *pre-request script* de la colección recalcula `{{fechaCita}}`
  al **próximo lunes** en cada envío, así ninguna cita cae en el pasado ni en domingo.
- **Test por petición:** cada una verifica su código de estado. Si ejecuta la colección
  completa con el *Collection Runner*, obtiene además una pantalla de resumen con los 79
  resultados — buena captura de cierre para el informe.

### 1.4 Correspondencia con los códigos de esta guía

Los nombres en Postman siguen el formato `C09 · POST /api/citas → 201`, con el mismo código que
esta guía. Cuatro peticiones de la colección no tienen sección propia aquí:

| Código | Qué es |
|---|---|
| `A06a` | Login del recepcionista, paso previo al 403 de `A06`. Sin captura |
| `D02b` | Restaura la contraseña del admin tras `D02`. Captura opcional |
| `E05b` | `GET` del paciente recién borrado → 404. Va junto a `E05` |
| — | El resto coincide uno a uno con los códigos `A01`–`F17` |

---

## 2. Cómo debe verse cada captura

Toda captura debe permitir verificar, sin ampliar la imagen, estos cinco elementos:

1. **Método y URL completa** (`POST http://localhost:5080/api/citas`).
2. **La pestaña relevante de la petición**: `Body` en POST/PUT/PATCH, `Params` cuando use
   filtros, `Headers`/`Authorization` en las pruebas de token.
3. **El código de estado** (`201 Created`, `409 Conflict`…), visible en la barra de la respuesta.
4. **Tiempo y tamaño** de la respuesta — evidencian que la petición se ejecutó de verdad.
5. **El cuerpo de la respuesta** en formato `Pretty` / `JSON`.

Ajustes que ayudan:

- **View → Layout → Two-pane view**: deja la petición a la izquierda y la respuesta a la
  derecha, de modo que *body enviado* y *body recibido* caben en una sola imagen. Es la
  configuración recomendada para todo el documento.
- Si el JSON de respuesta es largo (por ejemplo `GET /api/citas`), no hace falta que salga
  completo: basta el inicio del arreglo. Si el profesor pide el detalle, agregue una segunda
  captura con el scroll hacia abajo.
- Capture la **ventana completa** de Postman, no un recorte del panel: así se ve el
  environment seleccionado y la carpeta de la colección.
- En Windows: `Win + Shift + S` (recorte) o `Alt + Impr Pant` (ventana activa).

**Nombre de archivo:** `<código>-<método>-<recurso>-<caso>.png`, por ejemplo
`A01-post-auth-login-exitoso.png`, `F08-post-citas-fecha-pasada-400.png`. Guarde todo en
`docs/capturas/`. El código es el que aparece en las tablas siguientes.

**Nota sobre el token:** el JWT aparecerá visible en algunas capturas. No hay problema: es un
token local de desarrollo, firmado con la clave de `appsettings.json` y con 4 horas de vigencia.

---

## 3. Orden de ejecución

El orden importa: hay que crear antes de actualizar, y actualizar antes de eliminar.

```
A01 → A02 → A03 → A04 → A05        Autenticación y fallos de token
C01 → A06                          Crear usuario recepcionista y probar el 403
B01 … B24                          Todas las consultas (no modifican nada)
C02 … C09                          Todos los registros
D01 … D11                          Todas las actualizaciones
F01 … F17                          Validaciones y errores
E01 … E09                          Todas las eliminaciones (al final)
```

---

## 4. Bloque A — Autenticación

### A01 · Login exitoso → `200 OK`

`POST {{baseUrl}}/api/auth/login` · **Authorization: No Auth** · Body → raw → JSON:

```json
{
  "nombreUsuario": "admin",
  "password": "Admin123*"
}
```

**Evidencia:** la API valida credenciales contra el hash BCrypt y emite el JWT.
En la respuesta deben verse `datos.token`, `datos.tipoToken: "Bearer"`, `datos.expiraEn` y el
objeto `datos.usuario` **sin ningún campo de contraseña**. Deje visible el script de
`Post-response` o la variable `token` ya poblada.

### A02 · Perfil del usuario autenticado → `200 OK`

`GET {{baseUrl}}/api/auth/perfil`

**Evidencia:** el endpoint lee el `id` desde el claim del token, no de la URL. Capture también
la pestaña **Authorization** mostrando `Bearer {{token}}`.

### A03 · Credenciales incorrectas → `401 Unauthorized`

`POST {{baseUrl}}/api/auth/login` · **No Auth**

```json
{
  "nombreUsuario": "admin",
  "password": "ClaveIncorrecta1"
}
```

**Evidencia:** `"Usuario o contraseña incorrectos."` — el mismo mensaje que si el usuario no
existiera, para no revelar cuál de los dos falló.

### A04 · Petición sin token → `401 Unauthorized`

`GET {{baseUrl}}/api/citas` · **Authorization → No Auth**

**Evidencia:** `"Debe iniciar sesión y enviar el token JWT en el encabezado Authorization."`
Note que el error mantiene el mismo contrato `{ exito, mensaje }` del resto de la API.

### A05 · Token inválido o expirado → `401 Unauthorized`

`GET {{baseUrl}}/api/citas` · **Authorization → Bearer Token → `abc.123.xyz`**

**Evidencia:** `"El token enviado no es válido o ya expiró."` Capture la pestaña
**Authorization** con el token falso a la vista.

### A06 · Rol sin permiso → `403 Forbidden`

Requiere haber ejecutado **C01**. Primero inicie sesión con el usuario creado:

`POST {{baseUrl}}/api/auth/login` · **No Auth** · script `pm.environment.set("tokenRecepcion", pm.response.json().datos.token);`

```json
{
  "nombreUsuario": "recepcion.demo",
  "password": "Recepcion123*"
}
```

Luego: `POST {{baseUrl}}/api/usuarios` · **Authorization → Bearer Token → `{{tokenRecepcion}}`**

```json
{
  "nombreUsuario": "prueba.rol",
  "nombreCompleto": "Usuario Prueba de Rol",
  "correo": "prueba.rol@consultoriodental.com",
  "password": "Prueba123*",
  "rol": "Recepcionista"
}
```

**Evidencia:** `"Su rol no tiene permiso para ejecutar esta operación."` El token es válido
(no es un 401): lo que falla es la autorización por rol. Es la diferencia entre autenticación
y autorización, y conviene señalarla en el pie de la captura.

---

## 5. Bloque B — Consultas

Todas son `GET` con el token de administrador heredado. Ninguna modifica datos.

| # | Petición | Qué evidencia |
|---|---|---|
| B01 | `GET {{baseUrl}}/api/usuarios` | Listado completo; ningún `passwordHash` en la salida |
| B02 | `GET {{baseUrl}}/api/usuarios/1` | Consulta por ID con `rolNombre` y `ultimoAcceso` |
| B03 | `GET {{baseUrl}}/api/usuarios?activo=true&busqueda=admin` | Filtro + búsqueda parcial (capture la pestaña **Params**) |
| B04 | `GET {{baseUrl}}/api/pacientes` | Listado ordenado por apellido, con `edad` y `totalCitas` calculados |
| B05 | `GET {{baseUrl}}/api/pacientes/1` | `nombreCompleto` y `edad` derivados por la API, no almacenados |
| B06 | `GET {{baseUrl}}/api/pacientes?busqueda=001-1234567-8` | Búsqueda por documento |
| B07 | `GET {{baseUrl}}/api/dentistas` | Cada dentista con su especialidad y sus 11 horarios anidados |
| B08 | `GET {{baseUrl}}/api/dentistas/1` | Consulta por ID |
| B09 | `GET {{baseUrl}}/api/dentistas?activo=true&especialidadId=2` | Filtro combinado: devuelve solo a Rafael Peña |
| B10 | `GET {{baseUrl}}/api/dentistas/1/disponibilidad?fecha={{fechaCita}}` | **Endpoint calculado:** bloques de trabajo del día, citas ya ocupadas y minutos ocupados |
| B11 | `GET {{baseUrl}}/api/especialidades` | Catálogo con `totalDentistas` por especialidad |
| B12 | `GET {{baseUrl}}/api/especialidades/1` | Consulta por ID |
| B13 | `GET {{baseUrl}}/api/motivos` | Catálogo ordenado por prioridad, con `prioridadNombre` |
| B14 | `GET {{baseUrl}}/api/motivos/1` | Consulta por ID |
| B15 | `GET {{baseUrl}}/api/servicios` | Catálogo con precio y duración sugerida |
| B16 | `GET {{baseUrl}}/api/servicios/1` | Consulta por ID |
| B17 | `GET {{baseUrl}}/api/consultorios` | Catálogo de salas con `totalCitas` |
| B18 | `GET {{baseUrl}}/api/consultorios/1` | Consulta por ID |
| B19 | `GET {{baseUrl}}/api/horarios-dentista?dentistaId=1&diaSemana=1` | Horarios del lunes del dentista 1, con `minutosDisponibles` |
| B20 | `GET {{baseUrl}}/api/horarios-dentista/1` | Consulta por ID |
| B21 | `GET {{baseUrl}}/api/citas` | **Entidad principal.** Cada cita trae `estado`, `estadoNombre`, `tiempoRestante` (días/horas/minutos + descripción) y `costoEstimado`, todos calculados |
| B22 | `GET {{baseUrl}}/api/citas/3` | Cita vigente: `tiempoRestante.descripcion` legible |
| B23 | `GET {{baseUrl}}/api/citas?estado=Vigente&dentistaId=3&fechaDesde=2026-01-01&fechaHasta=2026-12-31` | Filtro múltiple, incluyendo el estado **calculado** (no está en la base de datos) |
| B24 | `GET {{baseUrl}}/api/citas/resumen` | Totales por estado, citas de hoy e ingreso estimado pendiente |

Capturas opcionales que refuerzan la sección de cálculos automáticos: repita B21 con
`?estado=EnProceso` y con `?estado=Finalizada` para mostrar que el mismo dato produce estados
distintos según el momento de la consulta.

---

## 6. Bloque C — Registros (`POST` → `201 Created`)

En todas: **Body → raw → JSON**. Verifique que el estado sea **201**, no 200, y que en la
pestaña **Headers** de la respuesta aparezca `Location` con la URL del recurso creado — es la
prueba de que la API sigue la convención REST. Vale la pena que al menos una captura del
bloque muestre esa pestaña.

### C01 · Crear usuario (rol Administrador requerido)

`POST {{baseUrl}}/api/usuarios` → script: `usuarioDemoId`

```json
{
  "nombreUsuario": "recepcion.demo",
  "nombreCompleto": "Recepcionista de Demostración",
  "correo": "recepcion.demo@consultoriodental.com",
  "password": "Recepcion123*",
  "rol": "Recepcionista",
  "activo": true
}
```

**Evidencia:** la contraseña entra en texto plano y **no vuelve nunca** en la respuesta: se
guarda como hash BCrypt. Este usuario es el que usará en A06 para el 403.

### C02 · Registrar paciente

`POST {{baseUrl}}/api/pacientes` → script: `pacienteDemoId`

```json
{
  "nombre": "Pedro",
  "apellido": "Martínez",
  "documento": "402-1111111-1",
  "fechaNacimiento": "1992-04-18",
  "telefono": "809-555-7788",
  "correo": "pedro.martinez@correo.com",
  "direccion": "Calle El Sol 22, Santo Domingo",
  "alergias": "Ninguna conocida",
  "activo": true
}
```

**Evidencia:** la respuesta incluye `edad` y `nombreCompleto`, que la API calcula; el cliente
nunca los envía.

### C03 · Registrar especialidad

`POST {{baseUrl}}/api/especialidades` → script: `especialidadDemoId`

```json
{
  "nombre": "Odontología Estética",
  "descripcion": "Blanqueamiento, carillas y armonía de la sonrisa.",
  "activa": true
}
```

### C04 · Registrar dentista

`POST {{baseUrl}}/api/dentistas` → script: `dentistaDemoId`

```json
{
  "nombre": "Gabriel",
  "apellido": "Núñez",
  "numeroLicencia": "EXQ-40777",
  "correo": "gabriel.nunez@consultoriodental.com",
  "telefono": "809-555-0104",
  "especialidadId": {{especialidadDemoId}},
  "activo": true
}
```

**Evidencia:** la API resuelve la relación y devuelve `especialidadNombre`. Nace sin horarios
(`horarios: []`), lo que enlaza con C05.

### C05 · Registrar horario del dentista

`POST {{baseUrl}}/api/horarios-dentista` → script: `horarioDemoId`

```json
{
  "dentistaId": {{dentistaDemoId}},
  "diaSemana": 2,
  "horaInicio": "09:00:00",
  "horaFin": "13:00:00",
  "observacion": "Jornada de martes",
  "activo": true
}
```

**Evidencia:** `diaSemana: 2` se devuelve como `"Martes"` y la API calcula
`minutosDisponibles: 240`.

### C06 · Registrar motivo

`POST {{baseUrl}}/api/motivos` → script: `motivoDemoId`

```json
{
  "nombre": "Blanqueamiento dental",
  "descripcion": "Solicitud de aclaramiento del color dental.",
  "prioridad": 1,
  "activo": true
}
```

### C07 · Registrar servicio

`POST {{baseUrl}}/api/servicios` → script: `servicioDemoId`

```json
{
  "nombre": "Blanqueamiento en consultorio",
  "descripcion": "Sesión de blanqueamiento con lámpara LED.",
  "precio": 7500.00,
  "duracionMinutos": 60,
  "activo": true
}
```

### C08 · Registrar consultorio

`POST {{baseUrl}}/api/consultorios` → script: `consultorioDemoId`

```json
{
  "codigo": "C-04",
  "nombre": "Consultorio 4",
  "ubicacion": "Segundo nivel, ala este",
  "capacidad": 1,
  "activo": true
}
```

### C09 · Agendar cita (endpoint principal)

`POST {{baseUrl}}/api/citas` → script: `citaDemoId`

```json
{
  "pacienteId": {{pacienteDemoId}},
  "fecha": "{{fechaCita}}",
  "hora": "10:00:00",
  "duracionMinutos": 0,
  "dentistaId": 1,
  "motivoId": 2,
  "servicioId": 2,
  "consultorioId": 1,
  "notas": "Primera limpieza del paciente."
}
```

**Evidencia — la captura más importante del informe.** El cuerpo enviado **no incluye** estado,
costo ni duración, y la respuesta sí trae:

- `duracionMinutos: 45` → tomada del servicio porque se envió `0`;
- `costoEstimado: 2500.00` → precio del servicio congelado al agendar;
- `estado: "Vigente"` y `tiempoRestante` con días, horas, minutos y descripción;
- los nombres de paciente, dentista, especialidad, motivo, servicio y consultorio resueltos.

Antes de aceptarla, la API verificó: que las cinco entidades referenciadas existan y estén
activas, que la fecha sea futura, que la hora caiga dentro del horario del dentista y que no
haya solapamiento para dentista, consultorio ni paciente.

---

## 7. Bloque D — Actualizaciones

### D01 · Actualizar usuario → `PUT {{baseUrl}}/api/usuarios/{{usuarioDemoId}}` · `200 OK`

```json
{
  "nombreCompleto": "Recepcionista Principal de Demostración",
  "correo": "recepcion.demo@consultoriodental.com",
  "rol": "Recepcionista",
  "activo": true
}
```

**Evidencia:** el `PUT` de usuario **no admite contraseña**: cambiarla tiene su propio endpoint
(D02). Compare con la captura de C01.

### D02 · Cambiar la propia contraseña → `POST {{baseUrl}}/api/usuarios/cambiar-password` · `200 OK`

Ejecútelo con el token del **administrador** y devuelva la clave a su valor original justo
después, para no romper el resto de las capturas:

```json
{
  "passwordActual": "Admin123*",
  "passwordNuevo": "Admin456*"
}
```

Segunda captura (opcional pero recomendada): repetir invirtiendo los valores para restaurar
`Admin123*`. **Evidencia:** el usuario se toma del token; nadie puede cambiar la contraseña de
otro por esta vía.

### D03 · Actualizar paciente → `PUT {{baseUrl}}/api/pacientes/{{pacienteDemoId}}` · `200 OK`

```json
{
  "nombre": "Pedro",
  "apellido": "Martínez",
  "documento": "402-1111111-1",
  "fechaNacimiento": "1992-04-18",
  "telefono": "829-555-9900",
  "correo": "pedro.martinez@correo.com",
  "direccion": "Av. Winston Churchill 105, Santo Domingo",
  "alergias": "Alergia a la lidocaína",
  "activo": true
}
```

**Evidencia:** cambian teléfono, dirección y alergias; `totalCitas` ya refleja la cita creada
en C09.

### D04 · Actualizar dentista → `PUT {{baseUrl}}/api/dentistas/{{dentistaDemoId}}` · `200 OK`

```json
{
  "nombre": "Gabriel",
  "apellido": "Núñez",
  "numeroLicencia": "EXQ-40777",
  "correo": "gabriel.nunez@consultoriodental.com",
  "telefono": "809-555-9104",
  "especialidadId": {{especialidadDemoId}},
  "activo": true
}
```

### D05 · Actualizar especialidad → `PUT {{baseUrl}}/api/especialidades/{{especialidadDemoId}}` · `200 OK`

```json
{
  "nombre": "Odontología Estética",
  "descripcion": "Blanqueamiento, carillas, resinas estéticas y diseño de sonrisa.",
  "activa": true
}
```

### D06 · Actualizar horario → `PUT {{baseUrl}}/api/horarios-dentista/{{horarioDemoId}}` · `200 OK`

```json
{
  "dentistaId": {{dentistaDemoId}},
  "diaSemana": 2,
  "horaInicio": "08:00:00",
  "horaFin": "14:00:00",
  "observacion": "Jornada de martes ampliada",
  "activo": true
}
```

**Evidencia:** `minutosDisponibles` pasa de 240 a **360**: el recálculo es automático.

### D07 · Actualizar motivo → `PUT {{baseUrl}}/api/motivos/{{motivoDemoId}}` · `200 OK`

```json
{
  "nombre": "Blanqueamiento dental",
  "descripcion": "Solicitud de aclaramiento del color dental.",
  "prioridad": 2,
  "activo": true
}
```

**Evidencia:** `prioridadNombre` cambia de `"Baja"` a `"Media"`.

### D08 · Actualizar servicio → `PUT {{baseUrl}}/api/servicios/{{servicioDemoId}}` · `200 OK`

```json
{
  "nombre": "Blanqueamiento en consultorio",
  "descripcion": "Sesión de blanqueamiento con lámpara LED. Incluye control a los 15 días.",
  "precio": 8200.00,
  "duracionMinutos": 75,
  "activo": true
}
```

### D09 · Actualizar consultorio → `PUT {{baseUrl}}/api/consultorios/{{consultorioDemoId}}` · `200 OK`

```json
{
  "codigo": "C-04",
  "nombre": "Consultorio 4 - Estética",
  "ubicacion": "Segundo nivel, ala este",
  "capacidad": 2,
  "activo": true
}
```

### D10 · Reprogramar cita → `PUT {{baseUrl}}/api/citas/{{citaDemoId}}` · `200 OK`

```json
{
  "pacienteId": {{pacienteDemoId}},
  "fecha": "{{fechaCita}}",
  "hora": "15:00:00",
  "duracionMinutos": 60,
  "dentistaId": 1,
  "motivoId": 1,
  "servicioId": 3,
  "consultorioId": 2,
  "notas": "Reprogramada a la tarde a solicitud del paciente."
}
```

**Evidencia:** cambian hora, duración, servicio, motivo y consultorio; `costoEstimado` se
recalcula al precio del nuevo servicio (3,500) y `tiempoRestante` se ajusta. La API repitió
todas las validaciones de agenda antes de aceptar el cambio.

### D11 · Cancelar cita → `PATCH {{baseUrl}}/api/citas/3/cancelar` · `200 OK`

```json
{
  "motivoCancelacion": "El paciente notificó que viajará esa semana."
}
```

**Evidencia:** cancelar **no borra**. La cita sigue existiendo con `cancelada: true`,
`estado: "Cancelada"` y el motivo registrado — es una baja lógica, no física. Confirme con un
`GET /api/citas/3` posterior si quiere reforzarlo.

> Si repite la sesión de capturas sin recrear la base de datos, esta petición devolverá
> `400 · "La cita ya se encuentra cancelada."`. Es correcto, pero para la captura de D11 use
> una cita vigente.

---

## 8. Bloque E — Eliminaciones

Ejecútelas **en este orden**: la API impide borrar registros con dependencias, así que primero
va la cita, luego el horario, luego el dentista y por último la especialidad que lo respalda.

| # | Petición | Estado | Qué evidencia |
|---|---|---|---|
| E01 | `DELETE {{baseUrl}}/api/citas/{{citaDemoId}}` | `200` | Solo se eliminan citas que **no han iniciado**; respuesta `{ exito, mensaje }` sin `datos` |
| E02 | `DELETE {{baseUrl}}/api/horarios-dentista/{{horarioDemoId}}` | `200` | Se permite porque el bloque no tiene citas futuras dentro |
| E03 | `DELETE {{baseUrl}}/api/dentistas/{{dentistaDemoId}}` | `200` | Dentista sin citas registradas |
| E04 | `DELETE {{baseUrl}}/api/especialidades/{{especialidadDemoId}}` | `200` | Posible solo después de E03: ya no tiene dentistas asignados |
| E05 | `DELETE {{baseUrl}}/api/pacientes/{{pacienteDemoId}}` | `200` | Posible solo después de E01: quedó sin citas |
| E06 | `DELETE {{baseUrl}}/api/motivos/{{motivoDemoId}}` | `200` | Catálogo sin citas asociadas |
| E07 | `DELETE {{baseUrl}}/api/servicios/{{servicioDemoId}}` | `200` | Catálogo sin citas asociadas |
| E08 | `DELETE {{baseUrl}}/api/consultorios/{{consultorioDemoId}}` | `200` | Catálogo sin citas asociadas |
| E09 | `DELETE {{baseUrl}}/api/usuarios/{{usuarioDemoId}}` | `200` | Requiere rol Administrador; use el token de `admin`, no `{{tokenRecepcion}}` |

Buena práctica para el informe: acompañe **E05** con un `GET {{baseUrl}}/api/pacientes/{{pacienteDemoId}}`
inmediatamente posterior, que devolverá `404`. Esa pareja de capturas demuestra que la
eliminación fue efectiva y no solo un mensaje de éxito.

---

## 9. Bloque F — Validaciones y respuestas de error

Es el bloque que más peso tiene en la evaluación: demuestra que **ninguna excepción llega sin
controlar al cliente** y que todos los errores conservan el mismo contrato JSON.

### F01 · Varias validaciones de campo a la vez → `400 Bad Request`

`POST {{baseUrl}}/api/pacientes`

```json
{
  "nombre": "A",
  "apellido": "",
  "documento": "12*34",
  "fechaNacimiento": "2030-01-01",
  "telefono": "abc",
  "correo": "esto-no-es-un-correo"
}
```

**Evidencia:** el arreglo `errores` reúne **todas** las fallas en una sola respuesta —longitud
mínima, campo obligatorio, formato del documento, fecha futura, teléfono y correo— en lugar de
detenerse en la primera. Es la mejor captura para mostrar las *DataAnnotations* en acción.

### F02 · JSON mal formado → `400 Bad Request`

`POST {{baseUrl}}/api/pacientes` · Body → raw → JSON:

```text
{ "nombre": "Pedro", "apellido": }
```

**Evidencia:** `"El cuerpo de la solicitud no tiene un formato JSON válido…"`. El error nativo
del deserializador (en inglés, con posiciones de bytes) se sustituye por un mensaje claro en
español: no se filtra información técnica.

### F03 · Tipo de dato incorrecto → `400 Bad Request`

`POST {{baseUrl}}/api/servicios`

```json
{
  "nombre": "Servicio con precio inválido",
  "precio": "gratis",
  "duracionMinutos": 30
}
```

**Evidencia:** `"El campo 'precio' tiene un valor con formato incorrecto."`

### F04 · Recurso inexistente → `404 Not Found`

`GET {{baseUrl}}/api/pacientes/9999`

**Evidencia:** `"No existe un registro de paciente con el ID 9999."` Repítalo con
`DELETE {{baseUrl}}/api/citas/9999` si quiere mostrar que el 404 también aplica a las
operaciones de escritura.

### F05 · Documento duplicado → `409 Conflict`

`POST {{baseUrl}}/api/pacientes`

```json
{
  "nombre": "Otro",
  "apellido": "Paciente",
  "documento": "001-1234567-8",
  "fechaNacimiento": "1995-06-01",
  "telefono": "809-555-0000"
}
```

**Evidencia:** `"Ya existe un paciente registrado con el documento '001-1234567-8'."` — el
índice único se valida antes de llegar a la base de datos.

### F06 · Nombre de catálogo duplicado → `409 Conflict`

`POST {{baseUrl}}/api/especialidades`

```json
{
  "nombre": "Ortodoncia",
  "descripcion": "Intento de duplicado."
}
```

**Evidencia:** la misma regla se aplica de forma uniforme en todos los catálogos. Variante
opcional: `POST /api/dentistas` con `numeroLicencia: "EXQ-10234"` → licencia duplicada.

### F07 · Eliminar con dependencias → `409 Conflict`

`DELETE {{baseUrl}}/api/pacientes/1`

**Evidencia:** `"No se puede eliminar el paciente porque tiene N cita(s) registrada(s). Puede
desactivarlo en su lugar."` El mensaje no solo rechaza: **propone la alternativa correcta**.
Variantes: `DELETE /api/especialidades/1` (tiene dentistas), `DELETE /api/servicios/2` (tiene citas).

### F08 · Cita en el pasado → `400 Bad Request`

`POST {{baseUrl}}/api/citas`

```json
{
  "pacienteId": 4,
  "fecha": "2020-01-15",
  "hora": "10:00:00",
  "duracionMinutos": 30,
  "dentistaId": 1,
  "motivoId": 1,
  "servicioId": 1,
  "consultorioId": 1
}
```

**Evidencia:** `"No se puede agendar una cita en el pasado…"`, con la fecha y hora
interpretadas por la API. Variante: fecha a más de 2 años → *"…con más de 2 años de anticipación."*

### F09 · Fuera del horario del dentista → `400 Bad Request`

`POST {{baseUrl}}/api/citas` — un **domingo**, o un día laborable a las 07:00:

```json
{
  "pacienteId": 4,
  "fecha": "{{fechaCita}}",
  "hora": "07:00:00",
  "duracionMinutos": 30,
  "dentistaId": 1,
  "motivoId": 1,
  "servicioId": 1,
  "consultorioId": 1
}
```

**Evidencia:** el mensaje devuelve el rango solicitado y **enumera los horarios disponibles**
del dentista para ese día (`08:00-12:00, 14:00-18:00`). Si usa un domingo, el mensaje es
`"El dentista no tiene horario de atención los días Domingo."`

### F10 · Solapamiento del dentista → `409 Conflict`

Requiere la cita de C09, que tras D10 quedó el **{{fechaCita}} de 15:00 a 16:00 con el dentista
1 en el consultorio 2**. Ahora se intenta agendar **otro paciente con el mismo dentista** dentro
de ese rango:

```json
{
  "pacienteId": 4,
  "fecha": "{{fechaCita}}",
  "hora": "15:15:00",
  "duracionMinutos": 30,
  "dentistaId": 1,
  "motivoId": 1,
  "servicioId": 1,
  "consultorioId": 3
}
```

**Evidencia:** `"El dentista ya tiene la cita #N agendada el dd/MM/yyyy de 15:00 - 16:00."`
La detección es por **intersección de rangos**, no por hora exacta: 15:15 choca con 15:00–16:00.

> Si toma esta captura **antes** de ejecutar D10, la cita sigue a las 10:00 en el consultorio 1:
> use `"hora": "10:15:00"`.

### F11 · Solapamiento del consultorio → `409 Conflict`

Mismo escenario, pero con **otro dentista** y el **mismo consultorio** que ocupa la cita:

```json
{
  "pacienteId": 4,
  "fecha": "{{fechaCita}}",
  "hora": "15:15:00",
  "duracionMinutos": 30,
  "dentistaId": 2,
  "motivoId": 1,
  "servicioId": 1,
  "consultorioId": 2
}
```

**Evidencia:** `"El consultorio 'C-02' está ocupado el dd/MM/yyyy de 15:00 - 16:00 (cita #N)."`
El dentista 2 está libre a esa hora: lo que bloquea es la sala. Junto con F10 demuestra que la
verificación cubre las tres dimensiones: dentista, consultorio y paciente.

> Antes de D10: use `"hora": "10:15:00"` y `"consultorioId": 1`.

### F12 · Modificar una cita ya finalizada → `400 Bad Request`

`PUT {{baseUrl}}/api/citas/1` (la cita de ejemplo ocurrida hace tres días), con cualquier
cuerpo válido.

**Evidencia:** `"No se puede modificar una cita en estado 'Finalizada'."` El estado no está
almacenado: se calculó en el instante de la petición para decidir el rechazo.

### F13 · Eliminar una cita ya finalizada → `409 Conflict`

`DELETE {{baseUrl}}/api/citas/1`

**Evidencia:** `"…Solo pueden eliminarse citas que aún no han iniciado."` Las citas atendidas
son historial clínico. Contrasta directamente con la captura E01, donde sí se elimina.

### F14 · Contraseña actual incorrecta → `400 Bad Request`

`POST {{baseUrl}}/api/usuarios/cambiar-password`

```json
{
  "passwordActual": "ClaveQueNoEs",
  "passwordNuevo": "NuevaClave123*"
}
```

**Evidencia:** `"La contraseña actual no es correcta."`

### F15 · Eliminar la propia cuenta → `400 Bad Request`

`DELETE {{baseUrl}}/api/usuarios/1` con el token de `admin` (usuario 1).

**Evidencia:** `"Un usuario no puede eliminar su propia cuenta mientras está autenticado."`
La API cruza el ID de la ruta con el claim del token.

### F16 · Rango de fechas inválido → `400 Bad Request`

`GET {{baseUrl}}/api/citas?fechaDesde=2026-12-31&fechaHasta=2026-01-01`

**Evidencia:** los parámetros de consulta también se validan:
`"La fecha inicial del filtro no puede ser posterior a la fecha final."`

### F17 · Horarios solapados del mismo dentista → `409 Conflict`

`POST {{baseUrl}}/api/horarios-dentista`

```json
{
  "dentistaId": 1,
  "diaSemana": 1,
  "horaInicio": "11:00:00",
  "horaFin": "15:00:00",
  "observacion": "Bloque que se solapa"
}
```

**Evidencia:** `"El horario se solapa con otro bloque del mismo dentista el Lunes (08:00 - 12:00)."`
El bloque 11:00–15:00 invade tanto la jornada matutina como la vespertina; el mensaje señala el
primer bloque en conflicto. Variante para `400`: enviar `horaFin` anterior a `horaInicio`, o un
bloque de menos de 15 minutos.

---

## 10. Matriz de cobertura

Tabla para el informe: demuestra de un vistazo que **los 51 endpoints** quedaron evidenciados.

| # | Método | Endpoint | Captura(s) |
|---|---|---|---|
| 1 | POST | `/api/auth/login` | A01, A03 |
| 2 | GET | `/api/auth/perfil` | A02 |
| 3 | GET | `/api/usuarios` | B01, B03 |
| 4 | GET | `/api/usuarios/{id}` | B02 |
| 5 | POST | `/api/usuarios` | C01, A06 |
| 6 | PUT | `/api/usuarios/{id}` | D01 |
| 7 | DELETE | `/api/usuarios/{id}` | E09, F15 |
| 8 | POST | `/api/usuarios/cambiar-password` | D02, F14 |
| 9 | GET | `/api/pacientes` | B04, B06 |
| 10 | GET | `/api/pacientes/{id}` | B05, F04 |
| 11 | POST | `/api/pacientes` | C02, F01, F02, F05 |
| 12 | PUT | `/api/pacientes/{id}` | D03 |
| 13 | DELETE | `/api/pacientes/{id}` | E05, F07 |
| 14 | GET | `/api/dentistas` | B07, B09 |
| 15 | GET | `/api/dentistas/{id}` | B08 |
| 16 | GET | `/api/dentistas/{id}/disponibilidad` | B10 |
| 17 | POST | `/api/dentistas` | C04 |
| 18 | PUT | `/api/dentistas/{id}` | D04 |
| 19 | DELETE | `/api/dentistas/{id}` | E03 |
| 20 | GET | `/api/horarios-dentista` | B19 |
| 21 | GET | `/api/horarios-dentista/{id}` | B20 |
| 22 | POST | `/api/horarios-dentista` | C05, F17 |
| 23 | PUT | `/api/horarios-dentista/{id}` | D06 |
| 24 | DELETE | `/api/horarios-dentista/{id}` | E02 |
| 25 | GET | `/api/especialidades` | B11 |
| 26 | GET | `/api/especialidades/{id}` | B12 |
| 27 | POST | `/api/especialidades` | C03, F06 |
| 28 | PUT | `/api/especialidades/{id}` | D05 |
| 29 | DELETE | `/api/especialidades/{id}` | E04 |
| 30 | GET | `/api/motivos` | B13 |
| 31 | GET | `/api/motivos/{id}` | B14 |
| 32 | POST | `/api/motivos` | C06 |
| 33 | PUT | `/api/motivos/{id}` | D07 |
| 34 | DELETE | `/api/motivos/{id}` | E06 |
| 35 | GET | `/api/servicios` | B15 |
| 36 | GET | `/api/servicios/{id}` | B16 |
| 37 | POST | `/api/servicios` | C07, F03 |
| 38 | PUT | `/api/servicios/{id}` | D08 |
| 39 | DELETE | `/api/servicios/{id}` | E07 |
| 40 | GET | `/api/consultorios` | B17 |
| 41 | GET | `/api/consultorios/{id}` | B18 |
| 42 | POST | `/api/consultorios` | C08 |
| 43 | PUT | `/api/consultorios/{id}` | D09 |
| 44 | DELETE | `/api/consultorios/{id}` | E08 |
| 45 | GET | `/api/citas` | B21, B23, A04, A05, F16 |
| 46 | GET | `/api/citas/resumen` | B24 |
| 47 | GET | `/api/citas/{id}` | B22 |
| 48 | POST | `/api/citas` | C09, F08, F09, F10, F11 |
| 49 | PUT | `/api/citas/{id}` | D10, F12 |
| 50 | PATCH | `/api/citas/{id}/cancelar` | D11 |
| 51 | DELETE | `/api/citas/{id}` | E01, F13 |

### Cobertura por código de estado

| Código | Situación | Capturas |
|---|---|---|
| `200 OK` | Consultas, actualizaciones y eliminaciones exitosas | Bloques B, D, E |
| `201 Created` | Registros con encabezado `Location` | Bloque C |
| `400 Bad Request` | Validación de campos y reglas de negocio | F01, F02, F03, F08, F09, F12, F14, F15, F16 |
| `401 Unauthorized` | Sin token, token inválido, credenciales incorrectas | A03, A04, A05 |
| `403 Forbidden` | Rol sin permiso | A06 |
| `404 Not Found` | Recurso inexistente | F04 |
| `409 Conflict` | Duplicados, solapamientos y dependencias | F05, F06, F07, F10, F11, F13, F17 |

---

## 11. Cómo presentar las capturas en el informe

Una estructura que funciona bien:

1. **Portada de la sección** con la tabla de la matriz de cobertura (§10) para que el lector
   ubique cada evidencia.
2. **Una captura por página o dos por página**, cada una con un pie de figura del estilo:
   > *Figura 12 — `POST /api/citas` · 201 Created. El cliente no envía estado, costo ni
   > duración; la API los calcula y devuelve la cita agendada con su tiempo restante.*
3. **Agrupe por bloque** (Autenticación, Consultas, Registros, Actualizaciones, Eliminaciones,
   Validaciones) siguiendo el orden de esta guía.
4. En el bloque de validaciones, ponga **la captura del éxito junto a la del error**
   (E01 con F13, C02 con F05): el contraste es lo que evidencia la regla de negocio.
5. Cierre con la captura de **Swagger** (`http://localhost:5080/swagger`) mostrando el listado
   completo de endpoints y el botón **Authorize**, como resumen visual de la API.

---

## 12. Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| `{{baseUrl}}` aparece en rojo | Environment no seleccionado | Selecciónelo arriba a la derecha |
| Todo responde `401` | El token no se guardó | Vuelva a ejecutar el login y revise el script de *Post-response* |
| `500` en cualquier endpoint | SQL Server apagado | `docker compose up -d` y reinicie la API |
| La cita se rechaza siempre | La fecha ya pasó, es domingo o cae fuera de 08:00–12:00 / 14:00–18:00 | Actualice `{{fechaCita}}` a un lunes futuro |
| `Could not send request` en HTTPS | Certificado autofirmado | Use `http://localhost:5080` o desactive la verificación SSL |
| Los IDs no coinciden con la guía | Base de datos ya usada | `docker compose down -v` y vuelva a ejecutar la API |
