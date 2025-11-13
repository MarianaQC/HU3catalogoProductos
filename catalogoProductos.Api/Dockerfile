# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos de proyecto (.csproj) de cada capa
COPY ["catalogoProductos.sln", "./"]
COPY ["catalogoProductos.Api/catalogoProductos.Api.csproj", "catalogoProductos.Api/"]
COPY ["catalogoProductos.Application/catalogoProductos.Application.csproj", "catalogoProductos.Application/"]
COPY ["catalogoProductos.Infrastructure/catalogoProductos.Infrastructure.csproj", "catalogoProductos.Infrastructure/"]
COPY ["catalogoProductos.Domain/catalogoProductos.Domain.csproj", "catalogoProductos.Domain/"]

# Restaurar dependencias (a partir del .sln para resolver todas las referencias)
RUN dotnet restore "catalogoProductos.sln"

# Copiar todo el código fuente al contenedor
COPY . .

# Compilar y publicar la API
WORKDIR "/src/catalogoProductos.Api"
RUN dotnet publish "catalogoProductos.Api.csproj" -c Release -o /app/publish

# Etapa 2: runtime (solo lo necesario para ejecutar la app)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "catalogoProductos.Api.dll"]
