FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Placeholder image used only to validate container build/push pipeline wiring.
CMD ["sh", "-c", "echo 'Atlas pipeline validation image started.' && sleep infinity"]