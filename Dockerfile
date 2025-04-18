FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out

RUN dotnet dev-certs https

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443
EXPOSE 8080

ARG DOMAIN

COPY ./certs/${DOMAIN}/certificate.pfx /app/certificate.pfx

ENTRYPOINT ["dotnet", "fetcher-tester.dll"]
