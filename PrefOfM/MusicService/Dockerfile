FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MusicService/MusicService.csproj", "MusicService/"]
RUN dotnet restore "MusicService/MusicService.csproj"
COPY . .
WORKDIR "/src/MusicService"
RUN dotnet build "MusicService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MusicService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY MusicService/Scripts /app/Scripts
COPY MusicService/AuthKeys /app/AuthKeys
COPY --chown=1000:1000 MusicService/NeuroModel /app/NeuroModel
USER root

WORKDIR /app
RUN chmod -R 755 /app/AuthKeys && \
    mkdir -p /etc/ssl/certs/private && \
    chmod -R 755 /app/Scripts
COPY ./certs/pfx/musicservice.pfx /etc/ssl/certs/localhost.pfx

COPY ./certs/pfx/*.pfx /etc/ssl/certs/trusted
COPY ./certs/crt/*.crt /etc/ssl/certs
COPY ./certs/private/*.key /etc/ssl/certs/private

RUN update-ca-certificates

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/localhost.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password="1234"
ENV ASPNETCORE_URLS=http://*:5000;https://*:7000
ENTRYPOINT ["dotnet", "MusicService.dll"]