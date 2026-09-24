# 运行时镜像：官方 aspnet 基础层 + CI 发布的应用层
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY publish/ ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Experience.Api.dll"]
