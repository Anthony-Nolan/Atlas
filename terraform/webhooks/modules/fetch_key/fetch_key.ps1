$jsonpayload = [Console]::In.ReadLine()

$inputs = $jsonpayload | ConvertFrom-Json

$Body = @{
  client_id= $inputs.clientId
  client_secret= $inputs.clientSecret
  grant_type="client_credentials"
  scope="https://management.azure.com/.default"
}

$function_app_id = $inputs.functionAppId

$response = Invoke-WebRequest -URI https://login.microsoftonline.com/ukmarrow.org/oauth2/v2.0/token -method "post" -Body $Body


$response_body = $response.content | ConvertFrom-Json
$access_token = $response_body.access_token
$headers = "Authorization=Bearer $access_token"

$result = az rest --method post --uri $function_app_id/host/default/listKeys?api-version=2018-11-01 --query "masterKey" --headers $headers

$output = '{  ' + ('"key": {0}' -f($result)) + '  }'

$output