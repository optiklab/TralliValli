#!/bin/bash
# Bicep Template Validation Script
# This script validates Bicep templates without deploying to Azure

set -e

echo "================================================"
echo "TraliVali Bicep Template Validation"
echo "================================================"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ ERROR: Azure CLI is not installed"
    echo "   Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

echo "✅ Azure CLI is installed"
echo "   Version: $(az version --query '"azure-cli"' -o tsv)"
echo ""

# Validate main.bicep
echo "Validating main.bicep..."
if az bicep build --file main.bicep --stdout > /dev/null 2>&1; then
    echo "✅ main.bicep compiled successfully"
else
    echo "❌ main.bicep compilation failed"
    az bicep build --file main.bicep 2>&1
    exit 1
fi

# Validate main-with-rg.bicep
echo "Validating main-with-rg.bicep..."
if az bicep build --file main-with-rg.bicep --stdout > /dev/null 2>&1; then
    echo "✅ main-with-rg.bicep compiled successfully"
else
    echo "❌ main-with-rg.bicep compilation failed"
    az bicep build --file main-with-rg.bicep 2>&1
    exit 1
fi

# Validate storage-lifecycle.bicep
echo "Validating storage-lifecycle.bicep..."
if az bicep build --file storage-lifecycle.bicep --stdout > /dev/null 2>&1; then
    echo "✅ storage-lifecycle.bicep compiled successfully"
else
    echo "❌ storage-lifecycle.bicep compilation failed"
    az bicep build --file storage-lifecycle.bicep 2>&1
    exit 1
fi

echo ""
echo "================================================"
echo "✅ All templates validated successfully!"
echo "================================================"
echo ""
echo "Resources defined in main.bicep:"
grep "^resource " main.bicep | wc -l | xargs echo "  - Resources:"
grep "^module " main.bicep | wc -l | xargs echo "  - Modules:"
grep "^param " main.bicep | wc -l | xargs echo "  - Parameters:"
grep "^output " main.bicep | wc -l | xargs echo "  - Outputs:"
echo ""
echo "Ready for deployment!"
echo ""
echo "To deploy with resource group creation:"
echo "  az deployment sub create \\"
echo "    --location eastus \\"
echo "    --template-file main-with-rg.bicep \\"
echo "    --parameters @parameters-with-rg.dev.json"
echo ""
echo "To deploy to existing resource group:"
echo "  az deployment group create \\"
echo "    --resource-group tralivali-dev-rg \\"
echo "    --template-file main.bicep \\"
echo "    --parameters @parameters.dev.json"
echo ""
