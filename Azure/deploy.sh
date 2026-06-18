# Get AI connection string
AI_CONN=$(az monitor app-insights component show \
  --app "$AI" \
  --resource-group "$RG" \
  --query connectionString -o tsv)

# Apply app settings
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "$APP" \
  --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_CONN"

