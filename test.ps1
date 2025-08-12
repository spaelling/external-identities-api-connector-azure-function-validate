$code = 'TODO'
$Uri = 'https://api-connector-sssu-01-gjgmfagqekd0ghf0.westeurope-01.azurewebsites.net/api/SignUpValidation?code=$code'
$Password = Read-Host -AsSecureString -Prompt 'Enter your password'
$cred = [pscredential]::new('EGMONT', $Password)

#region body with valid domain
$body = @{
    email       = 'asp@powercon.dk'
    displayName = 'ASP Powercon'
} | ConvertTo-Json
#endregion

#region body with invalid domain
$body = @{
    email       = 'asp@apento.com'
    displayName = 'ASP | APENTO'
} | ConvertTo-Json
#endregion

$params = @{
    Uri            = $Uri
    Method         = 'POST'
    Authentication = 'Basic'
    Credential     = $cred
    ContentType    = 'application/json'
    Body           = $body
}

$Response = Invoke-RestMethod @params
Write-Host "Message: $($Response.userMessage), action: $($Response.action)" -ForegroundColor Green