
# 🔐 Sistema de Autenticación con JWT + Refresh Tokens (Clean Architecture)

Este proyecto implementa un **sistema de registro y login seguro en .NET** utilizando **Clean Architecture**.
Se incluye manejo de **JWT + Refresh Tokens**, eliminación de **tokens caducados**, **control de sesiones activas por usuario** y aplicación de **principios SOLID**.

---

## 📖 Características principales

✔ **Registro y login de usuarios**
✔ **Autenticación con JWT** (Access Token + Refresh Token)
✔ **Renovación de tokens** sin necesidad de volver a loguearse
✔ **Revocación de tokens** individuales o en lote (logout global)
✔ **Límite de sesiones por usuario**
✔ **Eliminación automática de tokens viejos**
✔ **Arquitectura limpia** (independencia de frameworks, capas desacopladas)
✔ **Patrones de diseño aplicados** (Repository, Use Case, Dependency Injection)

---

## 🏗️ Arquitectura del Proyecto

El proyecto sigue **Clean Architecture**, dividiendo responsabilidades en capas:

```plaintext
📦 src
 ┣ 📂 Application        # Lógica de aplicación (qué hace el sistema)
 ┃ ┣ 📂 Common           # Utilidades y clases base
 ┃ ┣ 📂 Dtos             # Data Transfer Objects (entrada/salida de datos)
 ┃ ┣ 📂 Mappings         # AutoMapper profiles (DTOs ↔ Entidades)
 ┃ ┗ 📂 UseCases         # Casos de uso (reglas de aplicación)
 ┃
 ┣ 📂 Domain             # Núcleo del negocio (qué es el sistema)
 ┃ ┣ 📂 Entities         # Entidades del dominio (Usuario, RefreshToken, etc.)
 ┃ ┣ 📂 Repositories     # Interfaces de acceso a datos
 ┃ ┗ 📂 Services         # Lógica de negocio y servicios de dominio
 ┃
 ┣ 📂 Infrastructure     # Implementación técnica (cómo funciona)
 ┃ ┣ 📂 Logging          # Implementaciones de logging
 ┃ ┣ 📂 Migrations       # Migraciones de EF Core
 ┃ ┣ 📂 Persistence      # DbContext y configuración de EF Core
 ┃ ┣ 📂 Repositories     # Repositorios concretos (impl. interfaces)
 ┃ ┣ 📂 Security         # Autenticación/JWT/Refresh Tokens
 ┃ ┣ 📂 Services         # Servicios concretos (Email, Storage, etc.)
 ┃ ┗ 📜 ServiceCollectionExtensions.cs # Configuración de DI
 ┃
 ┗ 📂 Presentation       # Entrada/Salida (API pública)
   ┣ 📂 Controllers      # Endpoints REST (Usuarios, Tokens, Emails, etc.)
   ┣ 📂 Filters          # Filtros globales (manejo de excepciones, validaciones)
   ┣ 📂 Middleware       # Middlewares (JWT, logging de requests, CORS, etc.)

```

---

## 🚀 Tecnologías utilizadas

* **.NET 8** (compatible con .NET 6/7)
* **Entity Framework Core** (persistencia)
* **JWT (Json Web Tokens)**
* **Refresh Tokens seguros** (almacenados en DB)
* **SQL Server / PostgreSQL** (configurable)
* **Docker** (despliegue rápido con `docker-compose`)

---

## ⚡ Ejecución local

1️⃣ Clonar el repositorio

```bash
git clone https://github.com/usuario/proyecto-auth.git
cd proyecto-auth
```

2️⃣ Aplicar migraciones

```bash
dotnet ef database update
```

3️⃣ Levantar el proyecto

```bash
dotnet run
```

---

## 🧪 Casos de uso implementados

* **LoginUseCase.cs** → genera access y refresh token
* **RefreshTokenUseCase.cs** → renueva access token usando refresh
* **RevokeTokenUseCase.cs** → invalida un refresh token específico
* **RevokeAllUserTokensUseCase.cs** → logout global (todas las sesiones)
* **ValidateTokenUseCase.cs** → validación de token JWT
* **CleanupUserTokensUseCase.cs** → elimina tokens caducados
* **CreateUsuarioUseCase.cs / UpdateUsuarioUseCase.cs / DeleteUsuarioUseCase.cs** → CRUD de usuarios

---

## 🔒 Seguridad

* **Access Token corto (~30min)**
* **Refresh Token persistente en DB con expiración (~7días)**
* **Revocación de tokens viejos en login múltiple**
* **Sesiones limitadas por usuario**

---

## 📜 Licencia

Este proyecto está bajo la licencia **MIT**.


