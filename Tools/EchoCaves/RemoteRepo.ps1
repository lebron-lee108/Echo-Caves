param([ValidateSet('Inspect','Rename')][string]$Mode='Inspect')
$ErrorActionPreference='Stop'
$env:GIT_TERMINAL_PROMPT='0'
$env:GCM_INTERACTIVE='never'
$ecCredentialLines = @('protocol=https','host=github.com','') | git credential fill 2>$null
if($LASTEXITCODE -ne 0){throw 'Existing Git credential unavailable; no token printed or new sign-in attempted.'}
$ecCredential=@{}
foreach($ecLine in $ecCredentialLines){if($ecLine -match '^([^=]+)=(.*)$'){$ecCredential[$Matches[1]]=$Matches[2]}}
if(!$ecCredential.password){throw 'Existing Git credential contains no password/token.'}
$ecHeaders=@{Authorization='Bearer '+$ecCredential.password;Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='Echo-Caves-C-Handoff'}
try{$ecRepo=Invoke-RestMethod -Uri 'https://api.github.com/repos/lebron-lee108/VP515' -Headers $ecHeaders}
catch{throw ('GitHub repository inspection failed: HTTP '+[int]$_.Exception.Response.StatusCode)}
if($ecRepo.owner.login -ne 'lebron-lee108'){throw 'Repository owner mismatch; stopped.'}
$ecBefore=[pscustomobject]@{id=$ecRepo.id;name=$ecRepo.full_name;private=$ecRepo.private;defaultBranch=$ecRepo.default_branch;admin=$ecRepo.permissions.admin;push=$ecRepo.permissions.push}
if($Mode -eq 'Rename' -and $ecRepo.name -ne 'Echo-Caves'){
 if(!$ecRepo.permissions.admin){throw 'No repository admin permission for rename.'}
 try{$ecRepo=Invoke-RestMethod -Method Patch -Uri ('https://api.github.com/repos/'+$ecRepo.full_name) -Headers $ecHeaders -ContentType 'application/json' -Body '{"name":"Echo-Caves"}'}
 catch{throw ('Rename failed: HTTP '+[int]$_.Exception.Response.StatusCode)}
 if($ecRepo.id -ne $ecBefore.id -or $ecRepo.private -ne $ecBefore.private){throw 'Repository identity/visibility changed unexpectedly.'}
 git remote set-url origin $ecRepo.clone_url
 if($LASTEXITCODE -ne 0){throw 'Local remote update failed.'}
}
[pscustomobject]@{before=$ecBefore;current=$ecRepo.full_name;url=$ecRepo.html_url;clone=$ecRepo.clone_url;id=$ecRepo.id;private=$ecRepo.private;defaultBranch=$ecRepo.default_branch} | ConvertTo-Json -Depth 4
$ecCredential.Clear();$ecCredentialLines=$null;$ecHeaders=$null
