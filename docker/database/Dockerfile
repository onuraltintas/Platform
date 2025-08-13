FROM mcr.microsoft.com/mssql/server:2022-latest

# Set environment variables
ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Developer
ENV MSSQL_AGENT_ENABLED=true

# Copy entrypoint script and set permissions
COPY --chmod=755 entrypoint.sh /entrypoint.sh

# Copy initialization script
COPY init.sql /docker-entrypoint-initdb.d/

# Copy seed data scripts
COPY seeds/ /docker-entrypoint-initdb.d/seeds/

# Expose port
EXPOSE 1433

# Set the entrypoint
ENTRYPOINT ["/entrypoint.sh"]
