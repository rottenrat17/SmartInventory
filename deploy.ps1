# Deploy.ps1
# This script ensures SQLite is correctly set up in Azure

# Create SQLite database if it doesn't exist
$dbPath = "SmartInventory.db"
if (-not (Test-Path $dbPath)) {
    Write-Output "Creating new SQLite database..."
    # The database will be created by EF Core when the application runs
}

# Make the database file writable
if (Test-Path $dbPath) {
    Write-Output "Setting permissions on SQLite database..."
    try {
        $acl = Get-Acl $dbPath
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone", "FullControl", "Allow")
        $acl.SetAccessRule($rule)
        Set-Acl $dbPath $acl
        Write-Output "Permissions set successfully."
    }
    catch {
        Write-Error "Failed to set permissions: $_"
    }
}

Write-Output "Deployment script completed." 