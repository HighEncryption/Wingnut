##############################################################################################
# Notify.ps1                                                                                 #
##############################################################################################
#
# Description:
# This is an example script for handling notifications from Wingnut. When a notification is sent
# in Wingnut, a PowerShell script can be called to perform user-specific actions. The script will
# be called by Wingnut when the notification occurs and will provide information about the type
# of notification and the device that the notification is for. 
# The Wingnut service will call the function below. The name of the function must match the 
# name below, along with the function parameters.
#
# Implementer Notes:
# The script is invoked asynchronously by Wingnut, so a delay in processing the script will
# not cause Wingnut to delay processing of other notifications. For this reasons, implementers
# must consider that multiple executions of this script may be called concurrently.
# Output from the script (Errors, Warning, Information, Verbose) are logged to the Debug log
# of Wingnut. To view the output of these messages, look in the Windows Event Viewer.

# In order to interact with the Ups object, load the library containing the type information.
Add-Type -Path "%ProgramFiles%\Wingnut\Wingnut.Data.dll"

# This methos will be invoked by Wingnut when a notification event occurs
function Receive-WingnutNotification(
    [Wingnut.Data.NotificationType]$notificationType, 
    [Wingnut.Data.Models.Ups]$ups)
{
    Write-Host "The status of device $($ups.Name) is $($ups.Status)."
}