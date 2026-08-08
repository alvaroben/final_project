# Guía rápida de capturas en Postman — 13 imágenes

Proyecto final INF-4318 · API REST — Gestión de Citas de Consultorio Dental

Trece capturas en un solo recorrido de unos 20 minutos. Cubren autenticación, consultas,
registros, actualizaciones, eliminaciones y respuestas de validación.

> ⚠️ **Si su enunciado dice "cada uno de los endpoints", esta guía no basta**: son 51 endpoints
> y aquí solo se evidencian los principales. Para la cobertura completa use
> [GUIA_CAPTURAS_POSTMAN_COMPLETA.md](GUIA_CAPTURAS_POSTMAN_COMPLETA.md) (77 capturas) junto con
> la colección ya armada de [`docs/postman/`](postman/). Esta versión corta sirve como anexo
> resumido o cuando el requisito es por categorías, no por endpoint.

---

## Antes de empezar (5 minutos)

**1. Levantar la API sobre una base de datos limpia:**

```bash
docker compose up -d
dotnet run --project src/ConsultorioDental.API
```

Los IDs de esta guía (paciente 1, cita 1…) son los de la carga inicial automática. Si ya usó
la base de datos, reinicie con `docker compose down -v && docker compose up -d`.

**2. Importar** los dos archivos de [`docs/postman/`](postman/) (**Import** → arrastrarlos).
Traen las peticiones ya escritas y el environment; las 13 de esta guía son las de código
`A01`, `A04`, `B21`, `B07`, `C02`, `C09`, `F10`, `D10`, `D11`, `E01`, `F07`, `F01` y `F04`.

**3. Seleccionar el environment** `Consultorio Dental — Local` arriba a la derecha, o
`{{baseUrl}}` saldrá en rojo. El token se guarda solo al ejecutar el login.

**4. Layout:** `View → Layout → Two-pane view`. Deja la petición a la izquierda y la respuesta a
la derecha, de modo que **el JSON enviado y el recibido caben en una sola imagen**. Es lo que
hace posible bajar a 13 capturas.

**Cada captura debe mostrar:** método y URL · el `Body` o los `Params` · el **código de estado** ·
el tiempo de respuesta · el JSON devuelto. Capture la ventana completa (`Alt + Impr Pant`).

**Nombre de archivo:** `01-login.png`, `02-sin-token-401.png`… Guárdelas en `docs/capturas/`.

---

## Las 13 capturas

Ejecútelas **en orden**: unas dependen de otras. Solo necesita crear **cuatro peticiones** en
Postman; las demás capturas se obtienen cambiando el cuerpo o el método en la misma pestaña.

---

### 🔑 Autenticación

#### 01 · Login → `200 OK`

`POST {{baseUrl}}/api/auth/login` · **Authorization: No Auth** · Body → raw → JSON:

```json
{ "nombreUsuario": "admin", "password": "Admin123*" }
```

En la pestaña **Scripts → Post-response** pegue esto para guardar el token automáticamente:

```javascript
pm.environment.set("token", pm.response.json().datos.token);
```

**Demuestra:** credenciales validadas contra hash BCrypt y emisión del JWT. Señale en la
respuesta el `token`, el `expiraEn` y que el objeto `usuario` **no incluye la contraseña**.

#### 02 · Petición sin token → `401 Unauthorized`

`GET {{baseUrl}}/api/citas` · **Authorization → No Auth**

**Demuestra:** los endpoints están protegidos. Mensaje: *"Debe iniciar sesión y enviar el token
JWT en el encabezado Authorization."* — el error conserva el mismo contrato JSON que el éxito.

> Vuelva a poner **Inherit auth from parent** en esta petición antes de seguir.

---

### 🔍 Consultas

#### 03 · Listar citas → `200 OK`

`GET {{baseUrl}}/api/citas`

**Demuestra:** la consulta de la entidad principal con **campos calculados por la API**, no
almacenados: `estado`, `estadoNombre`, `tiempoRestante` (días, horas, minutos y una descripción
legible) y los nombres de paciente, dentista, servicio y consultorio ya resueltos.

> Anote la **fecha y hora de la cita vigente** que aparece aquí: la usará más adelante.

#### 04 · Listar dentistas → `200 OK`

`GET {{baseUrl}}/api/dentistas`

**Demuestra:** una sola imagen evidencia **tres de las ocho tablas** del enunciado. Cada dentista
viene con su `especialidadNombre` resuelto (tabla Especialidad) y con el arreglo `horarios`
anidado, donde cada bloque trae `diaSemanaNombre` y `minutosDisponibles` (tabla Horario del
dentista). Despliegue el primer dentista para que se vea el anidamiento.

> Si prefiere mostrar los cálculos agregados, la alternativa es
> `GET {{baseUrl}}/api/citas/resumen` (totales por estado e ingreso estimado pendiente), pero
> entonces Dentista, Especialidad y Horario se quedan sin evidencia propia.

---

### ➕ Registros

#### 05 · Registrar paciente → `201 Created`

`POST {{baseUrl}}/api/pacientes`

```json
{
  "nombre": "Pedro",
  "apellido": "Martínez",
  "documento": "402-1111111-1",
  "fechaNacimiento": "1992-04-18",
  "telefono": "809-555-7788",
  "correo": "pedro.martinez@correo.com",
  "activo": true
}
```

**Demuestra:** estado **201** (no 200) y la **edad calculada** por la API a partir de la fecha de
nacimiento. Anote el `id` devuelto: es su `PACIENTE_ID`.

#### 06 · Agendar cita → `201 Created`

`POST {{baseUrl}}/api/citas` — **la captura más importante del informe.**

```json
{
  "pacienteId": PACIENTE_ID,
  "fecha": "2026-08-10",
  "hora": "10:00:00",
  "duracionMinutos": 0,
  "dentistaId": 1,
  "motivoId": 2,
  "servicioId": 2,
  "consultorioId": 1,
  "notas": "Primera limpieza del paciente."
}
```

⚠️ **La fecha debe ser futura, de lunes a sábado**, y la hora dentro de 08:00–12:00 o
14:00–18:00. Si `2026-08-10` ya pasó, use el próximo lunes.

**Demuestra:** el cuerpo enviado **no incluye estado, costo ni duración**, y la respuesta trae
`duracionMinutos: 45` (tomada del servicio porque se envió `0`), `costoEstimado: 2500.00`
(precio congelado al agendar), `estado: "Vigente"` y el `tiempoRestante`. Antes de aceptarla la
API verificó que las cinco entidades existan y estén activas, que la fecha sea futura, que la
hora caiga en el horario del dentista y que no haya solapamiento.

Anote el `id` de la cita: es su `CITA_ID`.

#### 07 · Cita solapada → `409 Conflict`

**Pulse `Send` otra vez** en la misma petición 06, sin cambiar nada.

**Demuestra:** control de agenda. Mensaje: *"El dentista ya tiene la cita #N agendada el
10/08/2026 de 10:00 - 10:45."* La verificación es por intersección de rangos y cubre las tres
dimensiones: dentista, consultorio y paciente.

---

### ✏️ Actualizaciones

#### 08 · Reprogramar cita → `200 OK`

`PUT {{baseUrl}}/api/citas/CITA_ID`

```json
{
  "pacienteId": PACIENTE_ID,
  "fecha": "2026-08-10",
  "hora": "15:00:00",
  "duracionMinutos": 60,
  "dentistaId": 1,
  "motivoId": 1,
  "servicioId": 3,
  "consultorioId": 2,
  "notas": "Reprogramada a la tarde a solicitud del paciente."
}
```

**Demuestra:** cambian hora, duración, servicio y consultorio; el `costoEstimado` se **recalcula**
al precio del nuevo servicio (3,500) y el `tiempoRestante` se ajusta. La API repitió todas las
validaciones de agenda antes de aceptar el cambio.

#### 09 · Cancelar cita → `200 OK`

`PATCH {{baseUrl}}/api/citas/CITA_ID/cancelar`

```json
{ "motivoCancelacion": "El paciente notificó que viajará esa semana." }
```

**Demuestra:** cancelar **no borra**. La cita sigue existiendo con `cancelada: true`,
`estado: "Cancelada"` y el motivo registrado: es una baja lógica, no física.

---

### 🗑️ Eliminaciones

#### 10 · Eliminar cita → `200 OK`

`DELETE {{baseUrl}}/api/citas/CITA_ID`

**Demuestra:** eliminación efectiva de un registro que aún no ha iniciado. La respuesta es
`{ exito, mensaje }` sin `datos`.

#### 11 · Eliminar con dependencias → `409 Conflict`

`DELETE {{baseUrl}}/api/pacientes/1`

**Demuestra:** integridad referencial. Mensaje: *"No se puede eliminar el paciente porque tiene
N cita(s) registrada(s). Puede desactivarlo en su lugar."* — el error no solo rechaza:
**propone la alternativa correcta**. Colóquela junto a la captura 10 en el informe: el contraste
entre el borrado permitido y el bloqueado es lo que evidencia la regla.

---

### ⚠️ Validaciones

#### 12 · Datos inválidos → `400 Bad Request`

`POST {{baseUrl}}/api/pacientes` (reutilice la pestaña de la captura 05):

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

**Demuestra:** el arreglo `errores` reúne **todas** las fallas en una sola respuesta —longitud
mínima, campo obligatorio, formato de documento, fecha futura, teléfono y correo— en vez de
detenerse en la primera. Es la captura que evidencia las validaciones por campo.

#### 13 · Recurso inexistente → `404 Not Found`

`GET {{baseUrl}}/api/pacientes/9999`

**Demuestra:** *"No existe un registro de paciente con el ID 9999."* Ninguna excepción llega sin
controlar al cliente; el mensaje es claro y no filtra detalle técnico.

---

## Cobertura para el informe

Tabla para poner al inicio de la sección de evidencias:

| Requisito del enunciado | Capturas | Códigos evidenciados |
|---|---|---|
| **Autenticación** | 01, 02 | `200`, `401` |
| **Consultas** | 03, 04 | `200` |
| **Registros** | 05, 06 | `201` |
| **Actualizaciones** | 08, 09 | `200` |
| **Eliminaciones** | 10, 11 | `200`, `409` |
| **Respuestas de validación** | 07, 11, 12, 13 | `400`, `404`, `409` |

### Las 8 tablas del enunciado, y dónde se ven

Ponga también esta tabla: es la que responde por adelantado al *"¿y las demás tablas?"*.

| Tabla exigida | Dónde queda evidenciada |
|---|---|
| **Cita** *(principal)* | Capturas 03, 06, 07, 08, 09, 10 — CRUD completo, estado y tiempo restante calculados |
| **Paciente** | Capturas 05, 11, 12, 13 — alta, borrado bloqueado y validaciones |
| **Dentista** | Captura 04 — listado propio |
| **Especialidad** | Captura 04 — `especialidadNombre` resuelto en cada dentista |
| **Horario del dentista** | Captura 04 — arreglo `horarios` anidado, con minutos calculados |
| **Motivo** | Captura 06 — `motivoNombre` resuelto en la cita creada |
| **Servicio** | Captura 06 — `servicioNombre` y el `costoEstimado` tomado de su precio |
| **Consultorio** | Captura 06 — `consultorioNombre` resuelto en la cita creada |

La captura **06** es la clave del argumento: la respuesta de una sola cita resuelve **las ocho
tablas a la vez**, porque la API devuelve los nombres de paciente, dentista, especialidad,
motivo, servicio y consultorio junto con los campos calculados. Déjelo dicho en el pie de figura.

### Dos imágenes más que cierran cualquier duda de cobertura

No son capturas de ejecución, así que cuestan un minuto cada una:

1. **Swagger** en `http://localhost:5080/swagger`, con la lista de endpoints desplegada y el
   botón **Authorize** visible: documenta de un vistazo los 51 endpoints de la API.
2. **La colección importada en Postman**, con el panel lateral abierto mostrando las seis
   carpetas y sus peticiones. Y en el informe, una línea: *"la colección completa se entrega en
   `docs/postman/` y es ejecutable"*. El profesor puede correr él mismo lo que no esté capturado.

---

## Capturas opcionales, si le sobra tiempo

En orden de valor para la nota:

| Petición | Estado | Aporta |
|---|---|---|
| `POST /api/usuarios` con token de un usuario Recepcionista | `403` | Diferencia entre autenticación y autorización por rol |
| `GET /api/dentistas/1/disponibilidad?fecha=2026-08-10` | `200` | Bloques de trabajo y horas ocupadas calculados |
| `POST /api/citas` con `"fecha": "2020-01-15"` | `400` | No se agenda en el pasado |
| `POST /api/citas` con `"hora": "07:00:00"` | `400` | Fuera de horario; el mensaje lista los bloques disponibles |
| `DELETE /api/citas/1` (cita ya finalizada) | `409` | Las citas atendidas son historial clínico |
| `POST /api/pacientes` con `"documento": "001-1234567-8"` | `409` | Documento duplicado |

---

## Si algo falla

| Síntoma | Solución |
|---|---|
| `{{baseUrl}}` en rojo | Seleccione el environment arriba a la derecha |
| Todo responde `401` | Repita la captura 01; revise el script de *Post-response* |
| `500` en todo | SQL Server apagado: `docker compose up -d` y reinicie la API |
| La cita siempre se rechaza | La fecha ya pasó, es domingo, o está fuera de 08:00–12:00 / 14:00–18:00 |
| Los IDs no coinciden | Base de datos ya usada: `docker compose down -v` y reinicie |
