#!/bin/bash

# Set proper permissions for SQL Server data directory
if [ ! -d "/var/opt/mssql/data" ]; then
    mkdir -p /var/opt/mssql/data
fi

# Set ownership and permissions
chown -R mssql:mssql /var/opt/mssql
chmod -R 755 /var/opt/mssql

# Start SQL Server in background
/opt/mssql/bin/sqlservr &
SQLSERVER_PID=$!

# Wait for SQL Server to start
echo "Waiting for SQL Server to start..."
sleep 30

# Check if SQL Server is ready (with timeout)
TIMEOUT=300
COUNTER=0
echo "Checking if SQL Server is ready..."

until /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -C -Q "SELECT 1" &> /dev/null; do
    echo "SQL Server is not ready yet... (${COUNTER}/${TIMEOUT}s)"
    sleep 5
    COUNTER=$((COUNTER + 5))
    
    if [ $COUNTER -ge $TIMEOUT ]; then
        echo "Timeout waiting for SQL Server to start"
        exit 1
    fi
done

echo "SQL Server is ready!"

# Create a temporary init script with environment variables
if [ -f "/docker-entrypoint-initdb.d/init.sql" ]; then
    echo "Creating temporary init script with environment variables..."
    TEMP_INIT="/tmp/init_with_env.sql"
    
    # Replace placeholders with environment variables
    # Escape special characters in password for sed
    ESCAPED_PASSWORD=$(echo "$SA_PASSWORD" | sed 's/[[\.*^$()+?{|]/\\&/g')
    sed "s/\${DB_PASSWORD}/$ESCAPED_PASSWORD/g" /docker-entrypoint-initdb.d/init.sql > "$TEMP_INIT"
    
    echo "Running initialization script..."
    if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -C -i "$TEMP_INIT"; then
        echo "Initialization script completed successfully."
    else
        echo "Warning: Initialization script failed, but continuing..."
    fi
    
    # Clean up temporary file
    rm -f "$TEMP_INIT"
fi

# Run additional seed data scripts
if [ -d "/docker-entrypoint-initdb.d/seeds" ]; then
    echo "Running seed data scripts..."
    for script in /docker-entrypoint-initdb.d/seeds/*.sql; do
        if [ -f "$script" ]; then
            echo "Running seed script: $(basename "$script")"
            
            # Create temporary script with environment variables
            TEMP_SCRIPT="/tmp/$(basename "$script")"
            # Escape special characters in password for sed
            ESCAPED_PASSWORD=$(echo "$SA_PASSWORD" | sed 's/[[\.*^$()+?{|]/\\&/g')
            sed "s/\${DB_PASSWORD}/$ESCAPED_PASSWORD/g" "$script" > "$TEMP_SCRIPT"
            
            if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -C -i "$TEMP_SCRIPT"; then
                echo "Seed script $(basename "$script") completed successfully."
            else
                echo "Warning: Seed script $(basename "$script") failed, but continuing..."
            fi
            
            # Clean up temporary file
            rm -f "$TEMP_SCRIPT"
        fi
    done
    echo "Seed data scripts completed."
fi

echo "Database initialization completed successfully!"

# Wait for SQL Server process
wait $SQLSERVER_PID
