Param([Parameter(Mandatory=$True)][string]$subscriptionName, [Parameter(Mandatory=$True)][string]$deploymentLocation)

clear
Write-Host ''
Write-Host '----- Note -----' -ForegroundColor Green
Write-Host 'Please make sure to call the following command before proceeding...' -ForegroundColor Green
Write-Host 'Import-AzurePublishSettingsFile <path to your publish settings file downloaded from Azure>' -ForegroundColor Green
Write-Host 'IMPORTANT: make sure you run this in the root directory of the sample folder where the cspkg and cscfg files are located!!' -ForegroundColor Green
Write-Host '----------------' -ForegroundColor Green
Write-Host ''

#
# Try to find the configuration and package files
#

$cspkgPath = '.\AzureTrafficManagerApp.Azure.cspkg'
$cscfgPath = '.\ServiceConfiguration.Cloud.cscfg'

$cspkgExists = Test-Path $cspkgPath
$cscfgExists = Test-Path $cscfgPath

if(!$cscfgExists) {
	Write-Host 'Unable to find .\AzureTrafficManagerApp.Azure.cspkg' -ForegroundColor Red
	break
}

if(!$cscfgExists) {
	Write-Host 'Unable to find .\ServiceConfiguration.Cloud.cscfg' -ForegroundColor Red
	break
}

#
# Select the subscription based on the name
#
Write-Host 'Selecting subscription...'
$subscription = Get-AzureSubscription -SubscriptionName $subscriptionName
if(!$subscription) {
	Write-Host 'Unable to select subscription, cancelling setup!' -ForegroundColor Red
	break
}

#
# Next create the storage accounts if they do not exist
#
Write-Host 'Creating storage account...'
$storageAccountName = 'marioszp' + $deploymentLocation.Replace(' ', '').ToLower()
$storageAccount = Get-AzureStorageAccount -StorageAccountName $storageAccountName
if(!$storageAccount) {
	New-AzureStorageAccount -StorageAccountName $storageAccountName -Location $deploymentLocation 
	$storageAccount = Get-AzureStorageAccount -StorageAccountName
	if(!$storageAccount)
	{
		Write-Host 'Unable to create storage account, cancelling setup!' -ForegroundColor Red
		break
	}
}

#
# Next update the configuration files
#
Write-Host ''
Write-Host 'Updating configuration files...'

$serviceConfig = New-Object XML
$serviceConfig.Load($cscfgPath)
$serviceConfigNsManager = New-Object System.Xml.XmlNamespaceManager($serviceConfig.NameTable)
$serviceConfigNsManager.AddNamespace("p", "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration")



echo ''
echo 'Done!'
