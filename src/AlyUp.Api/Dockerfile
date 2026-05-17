FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/AlyUp.Api/AlyUp.Api.csproj", "src/AlyUp.Api/"]
COPY ["src/AlyUp.Application/AlyUp.Application.csproj", "src/AlyUp.Application/"]
COPY ["src/AlyUp.Domain/AlyUp.Domain.csproj", "src/AlyUp.Domain/"]
COPY ["src/AlyUp.Infrastructure/AlyUp.Infrastructure.csproj", "src/AlyUp.Infrastructure/"]

RUN dotnet restore "src/AlyUp.Api/AlyUp.Api.csproj"

COPY . .
WORKDIR /src/src/AlyUp.Api
RUN dotnet publish "AlyUp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AlyUp.Api.dll"]