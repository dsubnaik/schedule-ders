FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["schedule-ders/schedule-ders.csproj", "schedule-ders/"]
RUN dotnet restore "schedule-ders/schedule-ders.csproj"

COPY . .
WORKDIR /src/schedule-ders
RUN dotnet publish "schedule-ders.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet schedule-ders.dll"]
