# Variables
RG="rg-azuser7073_mml.local-46K7d"
ENV="prod"
REGION_CODE="cin"
INSTANCE="01"
LOC="centralindia"

AI="appi-${ENV}-${REGION_CODE}-${INSTANCE}"

# Delete RG
az group delete --name "$RG" --yes --no-wait

# Wait manually few seconds before next step if needed

# Recreate RG
az group create --name "$RG" --location "$LOC"

# Create App Insights
az monitor app-insights component create \
  --app "$AI" \
  --location "$LOC" \
  --resource-group "$RG" \
  --application-type web

# Get connection string
AI_CONN=$(az monitor app-insights component show \
  --app "$AI" \
  --resource-group "$RG" \
  --query connectionString -o tsv)

echo "Connection String:"
echo $AI_CONN