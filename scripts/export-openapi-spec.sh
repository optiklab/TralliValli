#!/bin/bash
# Script to export OpenAPI specification from the running API

set -e

API_URL="${API_URL:-http://localhost:5000}"
OUTPUT_FILE="${OUTPUT_FILE:-docs/openapi.json}"

echo "Exporting OpenAPI specification from $API_URL..."
echo "Output file: $OUTPUT_FILE"

# Check if the API is running
if ! curl -f -s "$API_URL/swagger/v1/swagger.json" > /dev/null 2>&1; then
    echo "Error: API is not accessible at $API_URL"
    echo "Please ensure the API is running before exporting the OpenAPI spec"
    echo "Start the API with: dotnet run --project src/TraliVali.Api/TraliVali.Api.csproj"
    exit 1
fi

# Download the OpenAPI spec
curl -s "$API_URL/swagger/v1/swagger.json" | jq '.' > "$OUTPUT_FILE"

if [ $? -eq 0 ]; then
    echo "✓ OpenAPI specification exported successfully to $OUTPUT_FILE"
    echo ""
    echo "To view the specification:"
    echo "  - Browse: $API_URL/swagger"
    echo "  - View JSON: $OUTPUT_FILE"
else
    echo "✗ Failed to export OpenAPI specification"
    exit 1
fi
