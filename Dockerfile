FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Oficina.API/Oficina.API.csproj", "Oficina.API/"]
RUN dotnet restore "Oficina.API/Oficina.API.csproj"

COPY . .
WORKDIR "/src/Oficina.API"
RUN dotnet publish "Oficina.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Oficina.API.dll"]