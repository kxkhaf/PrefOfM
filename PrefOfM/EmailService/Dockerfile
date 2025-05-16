FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5010
EXPOSE 7010

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EmailService/EmailService.csproj", "EmailService/"]
RUN dotnet restore "EmailService/EmailService.csproj"
COPY . .
WORKDIR "/src/EmailService"
RUN dotnet build "EmailService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EmailService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY EmailService/Templates /app/Templates

USER root
RUN chmod -R 755 /app/Templates
COPY ./certs/pfx/emailservice.pfx /etc/ssl/certs/localhost.pfx
COPY ./certs/pfx/*.pfx /etc/ssl/certs/trusted
COPY ./certs/crt/*.crt /etc/ssl/certs
COPY ./certs/private/*.key /etc/ssl/certs/private
RUN update-ca-certificates

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/localhost.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=1234
ENV ASPNETCORE_URLS=http://*:5010;https://*:7010

ENTRYPOINT ["dotnet", "EmailService.dll"]