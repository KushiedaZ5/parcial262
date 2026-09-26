FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PlataformaIncidencias.csproj", "./"]
RUN dotnet restore "./PlataformaIncidencias.csproj"

COPY . .
RUN dotnet publish "PlataformaIncidencias.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /data && chmod 777 /data

COPY --from=build /app/publish .

COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["./entrypoint.sh"]
