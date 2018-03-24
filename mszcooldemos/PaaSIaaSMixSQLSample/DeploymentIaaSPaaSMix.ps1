Param([Parameter(Mandatory=$True)][string]$serviceNamePrefix, [Parameter(Mandatory=$true)][string]$serviceLocation)

Write-Host ""
Write-Host "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - " -BackgroundColor Black -ForegroundColor Green
Write-Host "Starting deployment of " + $serviceNamePrefix + " in data center " + $serviceLocation -BackgroundColor Black -ForegroundColor Green
Write-Host "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - " -BackgroundColor Black -ForegroundColor Green
Write-Host ""

#
# Create the virtual network for the deployment
#

Get-AzureVNetConfig -ExportToFile "YourVnetConfig.netcfg"

# 
# Create the virtual machine inside of the network that runs SQL
#

#
# Deploy the bacpac into the SQL Server
#

#
# Configure SQL Server for SQL authentication
#

#
# Create a SQL Server Login for the application and give it access to the database
#

#
# Get the internal IP of the virtual machine
#

#
# Update the service configuration with the IP address
#


#
# Create the cloud service for the package and deploy the service
#