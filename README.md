# API REST — Gestión de Citas de Consultorio Dental

Proyecto final de la asignatura **INF-4318**. API REST desarrollada en **ASP.NET Core 10** con
**Entity Framework Core** sobre **SQL Server**, protegida con **JWT** y documentada con **Swagger**.

---

## 1. Contenido del proyecto

```text
final_project/
├── ConsultorioDental.sln
├── docker-compose.yml                  # SQL Server para desarrollo
└── src/ConsultorioDental.API/
    ├── Common/                         # Respuesta estándar, excepciones y validaciones propias
    ├── Controllers/                    # Endpoints REST (una clase por entidad)
    ├── Data/                           # DbContext y carga inicial de datos
    ├── DTOs/                           # Objetos de entrada y salida con sus validaciones
    ├── Middleware/                     # Manejo global de excepciones
    ├── Migrations/                     # Migraciones de EF Core
    ├── Models/                         # Entidades de la base de datos
    ├── Services/                       # Lógica de negocio y cálculos automáticos
    ├── ConsultorioDental.API.http      # Ejemplos de peticiones listos para ejecutar
    └── Program.cs                      # Configuración de la aplicación
```

---

## 2. Requisitos

- .NET SDK 10.0
- SQL Server (local, LocalDB o el contenedor Docker incluido)
- Opcional: `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

---

## 3. Cómo ejecutar

### 3.1 Levantar la base de datos

```bash
docker compose up -d
```

Esto expone SQL Server en `localhost:1433` con el usuario `sa` y la contraseña `Dental*2026Pass`.
Si ya cuenta con una instancia propia, ajuste la cadena `ConnectionStrings:ConexionSqlServer`
en `src/ConsultorioDental.API/appsettings.json`.

### 3.2 Ejecutar la API

```bash
dotnet run --project src/ConsultorioDental.API
```

Al iniciar, la aplicación **aplica las migraciones automáticamente** y carga datos de ejemplo
(especialidades, servicios, motivos, consultorios, dentistas con sus horarios, pacientes y tres citas
que ilustran los tres estados posibles).

El puerto está fijado en `Properties/launchSettings.json` y Swagger se abre solo al arrancar:

```text
http://localhost:5080/swagger
```

La raíz (`http://localhost:5080/`) también redirige a Swagger.

### 3.3 Iniciar sesión

Usuario administrador creado automáticamente:

| Usuario | Contraseña  | Rol           |
|---------|-------------|---------------|
| `admin` | `Admin123*` | Administrador |

1. Ejecute `POST /api/auth/login` con esas credenciales.
2. Copie el valor de `datos.token`.
3. Pulse **Authorize** en Swagger y pegue el token.
4. Todos los demás endpoints quedan habilitados.

---

## 4. Modelo de datos

| Tabla                | Descripción                                          | Relaciones |
|----------------------|------------------------------------------------------|------------|
| `Usuarios`           | Acceso al sistema (login y CRUD)                     | — |
| `Especialidades`     | Especialidad odontológica                            | 1 : N con Dentistas |
| `Dentistas`          | Profesional que atiende                              | N : 1 Especialidad · 1 : N Horarios · 1 : N Citas |
| `HorariosDentista`   | Bloques de disponibilidad por día de la semana       | N : 1 Dentista |
| `Pacientes`          | Información básica del paciente                      | 1 : N Citas |
| `Motivos`            | Razón de la consulta, con prioridad                  | 1 : N Citas |
| `Servicios`          | Servicio dental, con precio y duración sugerida      | 1 : N Citas |
| `Consultorios`       | Sala o área de atención                              | 1 : N Citas |
| `Citas`              | **Entidad principal**                                | FK a Paciente, Dentista, Motivo, Servicio y Consultorio |

Detalles de la configuración (en `Data/ConsultorioDbContext.cs`):

- Claves primarias y foráneas declaradas explícitamente con `HasOne / WithMany / HasForeignKey`.
- `DeleteBehavior.Restrict` en las relaciones de `Citas`: no se puede borrar un catálogo con citas asociadas.
- `DeleteBehavior.Cascade` solo en `HorariosDentista`, porque un horario no existe sin su dentista.
- Índices únicos en: usuario, correo de usuario, documento del paciente, licencia y correo del dentista,
  nombre de especialidad, motivo y servicio, y código de consultorio.
- Índices compuestos por `(Dentista, Fecha)`, `(Consultorio, Fecha)` y `(Paciente, Fecha)` para las
  verificaciones de solapamiento.

---

## 5. Cálculos automáticos

Ningún cliente envía el estado ni el tiempo restante: la API los deriva de `Fecha`, `Hora` y `Duración`.

**Estado de la cita** (`Models/Cita.cs`):

| Estado        | Condición |
|---------------|-----------|
| `Vigente`     | El momento actual es anterior al inicio |
| `EnProceso`   | El momento actual está entre el inicio y el fin (inicio + duración) |
| `Finalizada`  | El momento actual es posterior al fin |
| `Cancelada`   | La cita fue cancelada explícitamente |

**Días y horas restantes** (`Services/Mapeos.cs`): desglose en días, horas y minutos hasta el inicio,
más una descripción legible (`"Faltan 3 días, 20 horas, 29 minutos para la cita."`). Cuando la cita
está en proceso se devuelve además `minutosParaFinalizar`.

Otros cálculos:

- **Edad del paciente** a partir de la fecha de nacimiento.
- **Costo estimado** de la cita, tomado del precio del servicio al momento de agendar.
- **Duración**: si se envía `duracionMinutos = 0`, se usa la duración sugerida del servicio.
- **Resumen de agenda** (`GET /api/citas/resumen`): totales por estado, citas del día e ingreso
  estimado pendiente.

---

## 6. Endpoints

Todos requieren `Authorization: Bearer <token>`, excepto `POST /api/auth/login`.

### Autenticación
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/login` | Devuelve el JWT |
| GET  | `/api/auth/perfil` | Datos del usuario autenticado |

### Usuarios
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET    | `/api/usuarios` | Listar (filtros: `activo`, `busqueda`) |
| GET    | `/api/usuarios/{id}` | Consultar |
| POST   | `/api/usuarios` | Crear *(rol Administrador)* |
| PUT    | `/api/usuarios/{id}` | Actualizar *(rol Administrador)* |
| DELETE | `/api/usuarios/{id}` | Eliminar *(rol Administrador)* |
| POST   | `/api/usuarios/cambiar-password` | Cambiar la propia contraseña |

### Citas
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET    | `/api/citas` | Listar (filtros: `pacienteId`, `dentistaId`, `consultorioId`, `servicioId`, `motivoId`, `fechaDesde`, `fechaHasta`, `estado`) |
| GET    | `/api/citas/resumen` | Totales por estado e ingreso estimado |
| GET    | `/api/citas/{id}` | Consultar |
| POST   | `/api/citas` | Agendar |
| PUT    | `/api/citas/{id}` | Reprogramar o modificar |
| PATCH  | `/api/citas/{id}/cancelar` | Cancelar dejando el motivo |
| DELETE | `/api/citas/{id}` | Eliminar (solo si no ha iniciado) |

### Resto de entidades
`/api/pacientes`, `/api/dentistas`, `/api/especialidades`, `/api/motivos`, `/api/servicios`,
`/api/consultorios` y `/api/horarios-dentista` exponen el CRUD completo
(`GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`).

Adicional: `GET /api/dentistas/{id}/disponibilidad?fecha=YYYY-MM-DD` muestra los bloques de trabajo
y las citas ya ocupadas de ese día.

---

## 7. Validaciones y reglas de negocio

**Integridad de datos**

- No se permiten duplicados en los campos únicos (usuario, correo, documento, licencia, código de sala,
  nombres de catálogos) → **409 Conflict**.
- No se puede consultar, actualizar ni eliminar un ID inexistente → **404 Not Found**.
- No se puede eliminar un registro con dependencias (paciente con citas, especialidad con dentistas,
  servicio con citas…) → **409 Conflict**, sugiriendo desactivar en lugar de borrar.
- Tipos y formatos validados por campo: correo, teléfono, documento, longitudes, rangos numéricos,
  precios mayores que cero, prioridad entre 1 y 3, día de la semana entre 0 y 6.

**Fechas y horas**

- La fecha de nacimiento no puede ser futura ni anterior a 120 años.
- No se puede agendar una cita en el pasado, ni con más de 2 años de anticipación.
- La cita no puede extenderse más allá del final del día.
- La duración debe estar entre 5 y 480 minutos.
- Los segundos que envíe el cliente en la hora se descartan: la agenda trabaja al minuto.

**Agenda**

- La cita debe caer dentro de un bloque de horario activo del dentista para ese día de la semana;
  si no, el error indica los horarios disponibles.
- No puede haber solapamiento para el **mismo dentista**, el **mismo consultorio** ni el **mismo paciente**.
- Los bloques de horario de un dentista no pueden solaparse entre sí y deben durar al menos 15 minutos.
- No se elimina un horario que tenga citas futuras dentro de ese bloque.
- Una cita `EnProceso` o `Finalizada` no puede modificarse ni eliminarse (es historial clínico);
  una cita finalizada tampoco puede cancelarse.
- Solo se agendan citas con paciente, dentista, motivo, servicio y consultorio **activos**.

**Usuarios**

- Las contraseñas se guardan con hash **BCrypt**; nunca se devuelven en las respuestas.
- El login responde el mismo mensaje ante usuario inexistente o contraseña incorrecta.
- El sistema no permite quedarse sin administradores activos, ni que un usuario borre su propia cuenta.

---

## 8. Manejo de errores

`Middleware/ManejadorExcepcionesMiddleware.cs` envuelve todo el pipeline: **ninguna excepción llega
sin controlar al cliente** y el servicio nunca se interrumpe. Cada error se traduce a un código HTTP
y a un mensaje en español; el detalle técnico solo se escribe en el log del servidor (en `Development`
se adjunta también en la respuesta para facilitar la depuración).

| Situación | Código |
|-----------|--------|
| Datos inválidos o regla de negocio incumplida | 400 |
| Falta el token, es inválido o expiró | 401 |
| El rol no tiene permiso | 403 |
| ID inexistente | 404 |
| Duplicado, solapamiento o dependencia existente | 409 |
| Error inesperado | 500 (mensaje genérico) |

Todas las respuestas usan el mismo contrato:

```json
{
  "exito": true,
  "mensaje": "Cita agendada correctamente.",
  "datos": { }
}
```

```json
{
  "exito": false,
  "mensaje": "El dentista ya tiene la cita #4 agendada el 10/08/2026 de 10:00 - 10:45."
}
```

Los mensajes que genera internamente el deserializador de JSON se sustituyen por texto en español,
de modo que no se filtra información técnica.

---

## 9. Ejemplos rápidos

```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"nombreUsuario":"admin","password":"Admin123*"}' | jq -r '.datos.token')

# Citas con estado y tiempo restante calculados
curl -s http://localhost:5080/api/citas -H "Authorization: Bearer $TOKEN" | jq

# Agendar una cita
curl -s -X POST http://localhost:5080/api/citas \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"pacienteId":1,"fecha":"2026-08-10","hora":"10:00:00","duracionMinutos":0,
       "dentistaId":1,"motivoId":1,"servicioId":2,"consultorioId":1}' | jq
```

El archivo `src/ConsultorioDental.API/ConsultorioDental.API.http` contiene el resto de los ejemplos,
ejecutables desde Visual Studio, VS Code o Rider.

---

## 10. Comandos útiles

```bash
dotnet build                                                    # Compilar
dotnet run --project src/ConsultorioDental.API                  # Ejecutar
dotnet ef migrations add <Nombre> --project src/ConsultorioDental.API
dotnet ef database update --project src/ConsultorioDental.API
```
