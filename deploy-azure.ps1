# Clean and publish the main project
dotnet clean SmartInventoryManagement.csproj
dotnet publish SmartInventoryManagement.csproj -c Release -o ./publish

# Deploy to Azure using the Azure CLI
# Uncomment and modify this section if you're using Azure CLI
# az webapp deploy --resource-group <your-resource-group> --name SmartInventoryManagement --src-path ./publish

Write-Host "Project published to ./publish directory"
Write-Host "You can now deploy this package to Azure through the Azure portal or using the Azure CLI"
Write-Host "To deploy using the Azure CLI, uncomment and update the az webapp deploy command in this script" 