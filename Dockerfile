# The SDK image is around 800 MB and never ships. Only the published output crosses
# into the runtime image below, which is roughly a tenth of the size.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Project files first, on their own layer. Restore then re-runs only when a dependency
# changes, not on every source edit.
COPY Directory.Build.props ./
COPY src/TodoApp.Core/TodoApp.Core.csproj src/TodoApp.Core/
COPY src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj src/TodoApp.Infrastructure/
COPY src/TodoApp.Api/TodoApp.Api.csproj src/TodoApp.Api/
RUN dotnet restore src/TodoApp.Api/TodoApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/TodoApp.Api/TodoApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# The to-do file lives under a mount point, not inside an image layer, so replacing
# the container does not throw the list away.
ENV Storage__FilePath=/data/todos.json
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data

COPY --from=build /app .

# The image ships with a non-root user already created. Using it costs one line.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]