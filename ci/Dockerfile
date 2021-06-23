FROM mcr.microsoft.com/dotnet/aspnet:5.0

ADD ./out/app /app/

ENTRYPOINT cd /app && dotnet MyLab.PrometheusAgent.dll
