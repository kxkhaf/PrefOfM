FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AuthService/AuthService.csproj", "AuthService/"]
RUN dotnet restore "AuthService/AuthService.csproj"
COPY . .
WORKDIR "/src/AuthService"
RUN dotnet build "AuthService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AuthService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.dll"]
COPY ./AuthService/AuthKeys ./AuthKeys
USER root

WORKDIR /app

COPY ./certs/pfx/authservice.pfx /etc/ssl/certs/localhost.pfx

COPY ./certs/pfx/*.pfx /etc/ssl/certs/trusted
COPY ./certs/crt/*.crt /etc/ssl/certs
COPY ./certs/private/*.key /etc/ssl/certs/private

RUN update-ca-certificates

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/localhost.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=1234
ENV ASPNETCORE_URLS=http://*:5010;https://*:7010